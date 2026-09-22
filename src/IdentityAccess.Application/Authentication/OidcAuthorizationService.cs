using System.Text.RegularExpressions;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Implements strict public-client OpenID Connect Authorization Code + PKCE and rotating OAuth
    /// refresh-token grants on top of validated local-session and multi-database routing contracts.
    /// </summary>
    public sealed class OidcAuthorizationService(
        IAuthenticationClientRegistry clients,
        IAuthenticationDirectoryLocator directoryLocator,
        IOidcAuthorizationCodeStore authorizationCodes,
        IOidcRefreshTokenStore refreshTokens,
        IOidcCodeService codeService,
        IOidcRefreshTokenService refreshTokenService,
        IOidcTokenIssuer tokenIssuer,
        OidcOptions options,
        TimeProvider timeProvider,
        ISecurityAuditWriter auditWriter)
        : IOidcAuthorizationService
    {
        private static readonly Regex PkceChallengeSyntax =
            new(
                "^[A-Za-z0-9_-]{43}$",
                RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

        private static readonly Regex PkceVerifierSyntax =
            new(
                "^[A-Za-z0-9\\-._~]{43,128}$",
                RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

        private readonly OidcOptions validatedOptions =
            options.Validate();

        /// <inheritdoc />
        public OidcProviderMetadata Metadata =>
            new(
                validatedOptions.CanonicalIssuer,
                $"{validatedOptions.CanonicalIssuer}/connect/authorize",
                $"{validatedOptions.CanonicalIssuer}/connect/token",
                $"{validatedOptions.CanonicalIssuer}/.well-known/jwks.json");

        /// <inheritdoc />
        public OidcJsonWebKey SigningKey =>
            tokenIssuer.SigningKey;

        /// <inheritdoc />
        public IReadOnlyList<OidcJsonWebKey> SigningKeys =>
            tokenIssuer.SigningKeys;

        /// <inheritdoc />
        public async Task<OidcAuthorizationResult> AuthorizeAsync(
            OidcAuthorizationRequest request,
            AuthenticatedSessionContext? session,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.ClientId) ||
                !clients.TryGet(request.ClientId, out var client) ||
                !client.OidcEnabled)
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.UnknownClient);
            }

            if (string.IsNullOrWhiteSpace(request.RedirectUri) ||
                !client.AllowsRedirectUri(request.RedirectUri))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.RedirectUriRejected);
            }

            var redirectUri = request.RedirectUri;

            if (!ValidOpaqueState(request.State))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.InvalidRequest,
                    redirectUri);
            }

            var state = request.State!;

            if (!string.Equals(
                    request.ResponseType,
                    "code",
                    StringComparison.Ordinal))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.UnsupportedResponseType,
                    redirectUri,
                    state);
            }

            if (!string.Equals(
                    request.Scope,
                    "openid",
                    StringComparison.Ordinal) ||
                !client.AllowsOidcScope("openid"))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.InvalidScope,
                    redirectUri,
                    state);
            }

            if (!ValidNonce(request.Nonce))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.InvalidRequest,
                    redirectUri,
                    state);
            }

            if (!string.Equals(
                    request.CodeChallengeMethod,
                    "S256",
                    StringComparison.Ordinal))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.PkceRequired,
                    redirectUri,
                    state);
            }

            if (string.IsNullOrWhiteSpace(request.CodeChallenge) ||
                !PkceChallengeSyntax.IsMatch(request.CodeChallenge))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.InvalidPkce,
                    redirectUri,
                    state);
            }

            if (session is null ||
                !string.Equals(
                    session.ClientId,
                    client.ClientId,
                    StringComparison.Ordinal) ||
                session.Application != client.Application ||
                !string.Equals(
                    session.AuthenticationContextKey,
                    client.AuthenticationContextKey,
                    StringComparison.Ordinal))
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.LoginRequired,
                    redirectUri,
                    state);
            }

            AuthenticationDirectoryLocation location;

            try
            {
                location = await directoryLocator
                    .LocateAsync(
                        client.Application,
                        client.AuthenticationContextKey,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.DirectoryUnavailable,
                    redirectUri,
                    state);
            }

            if (location.Route.Request.IdentityScopeId !=
                session.Subject.IdentityScopeId)
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.LoginRequired,
                    redirectUri,
                    state);
            }

            var now =
                timeProvider.GetUtcNow();

            var issued =
                codeService.Issue();

            var grant =
                new OidcAuthorizationCodeGrant(
                    Guid.NewGuid(),
                    session.Subject,
                    session.SessionId,
                    client.ClientId,
                    client.Application,
                    client.AuthenticationContextKey,
                    redirectUri,
                    "openid",
                    request.CodeChallenge,
                    request.Nonce!,
                    session.AuthenticatedAt,
                    now,
                    now.AddSeconds(
                        validatedOptions.AuthorizationCodeLifetimeSeconds));

            var created =
                await authorizationCodes
                    .CreateForActiveSessionAsync(
                        location.Route,
                        grant,
                        issued.Hash,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!created)
            {
                return OidcAuthorizationResult.Reject(
                    OidcAuthorizationFailureCode.LoginRequired,
                    redirectUri,
                    state);
            }

            await auditWriter
                .TryWriteAsync(
                    location.Route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.OidcAuthorizationCodeIssued,
                        SecurityAuditOutcome.Succeeded,
                        session.Subject.IdentityScopeId,
                        userId: session.Subject.UserId,
                        application: client.Application,
                        clientId: client.ClientId,
                        targetId: grant.CodeId.ToString("D")),
                    cancellationToken)
                .ConfigureAwait(false);

            return OidcAuthorizationResult.Success(
                redirectUri,
                issued.Value,
                state);
        }

        /// <inheritdoc />
        public async Task<OidcTokenResult> ExchangeCodeAsync(
            OidcTokenRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(
                    request.GrantType,
                    "authorization_code",
                    StringComparison.Ordinal))
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.UnsupportedGrantType);
            }

            if (string.IsNullOrWhiteSpace(request.ClientId) ||
                !clients.TryGet(request.ClientId, out var client) ||
                !client.OidcEnabled)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidClient);
            }

            if (string.IsNullOrWhiteSpace(request.RedirectUri) ||
                !client.AllowsRedirectUri(request.RedirectUri))
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            if (string.IsNullOrWhiteSpace(request.Code) ||
                request.Code.Length != 43 ||
                request.Code.Any(
                    value =>
                        !(char.IsAsciiLetterOrDigit(value) ||
                            value is '-' or '_')))
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            if (string.IsNullOrWhiteSpace(request.CodeVerifier) ||
                !PkceVerifierSyntax.IsMatch(request.CodeVerifier))
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            AuthenticationDirectoryLocation location;

            try
            {
                location = await directoryLocator
                    .LocateAsync(
                        client.Application,
                        client.AuthenticationContextKey,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.DirectoryUnavailable);
            }

            var now =
                timeProvider.GetUtcNow();

            OidcAuthorizationCodeGrant? grant;

            try
            {
                grant = await authorizationCodes
                    .ConsumeAsync(
                        location.Route,
                        client.ClientId,
                        request.RedirectUri,
                        codeService.Hash(request.Code),
                        codeService.ComputeS256Challenge(
                            request.CodeVerifier),
                        now,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            if (grant is null)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            var issuedRefreshToken =
                refreshTokenService.Issue();

            var refreshGrant =
                new OidcRefreshTokenGrant(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    null,
                    0,
                    grant.Subject,
                    grant.SessionId,
                    grant.ClientId,
                    grant.Application,
                    grant.AuthenticationContextKey,
                    grant.Scope,
                    grant.AuthenticatedAt,
                    now,
                    now.AddDays(
                        validatedOptions.RefreshTokenLifetimeDays));

            var refreshFamilyCreated =
                await refreshTokens
                    .CreateFamilyForActiveSessionAsync(
                        location.Route,
                        refreshGrant,
                        issuedRefreshToken.Hash,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!refreshFamilyCreated)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            OidcIssuedTokens tokens;

            try
            {
                tokens = tokenIssuer.Issue(
                    new OidcTokenIssueRequest(
                        grant.Subject,
                        grant.SessionId,
                        grant.ClientId,
                        grant.Application,
                        grant.Scope,
                        grant.Nonce,
                        grant.AuthenticatedAt,
                        now));
            }
            catch
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.TokenIssuanceFailed);
            }

            await auditWriter
                .TryWriteAsync(
                    location.Route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.OidcAuthorizationCodeRedeemed,
                        SecurityAuditOutcome.Succeeded,
                        grant.Subject.IdentityScopeId,
                        userId: grant.Subject.UserId,
                        application: grant.Application,
                        clientId: grant.ClientId,
                        targetId: grant.CodeId.ToString("D")),
                    cancellationToken)
                .ConfigureAwait(false);

            await auditWriter
                .TryWriteAsync(
                    location.Route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.OidcRefreshTokenFamilyCreated,
                        SecurityAuditOutcome.Succeeded,
                        refreshGrant.Subject.IdentityScopeId,
                        userId: refreshGrant.Subject.UserId,
                        application: refreshGrant.Application,
                        clientId: refreshGrant.ClientId,
                        targetId: refreshGrant.FamilyId.ToString("D")),
                    cancellationToken)
                .ConfigureAwait(false);

            return OidcTokenResult.AuthorizationCodeSuccess(
                tokens,
                issuedRefreshToken.Value,
                grant.Scope);
        }

        /// <inheritdoc />
        public async Task<OidcTokenResult> RefreshAsync(
            OidcRefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.ClientId) ||
                !clients.TryGet(request.ClientId, out var client) ||
                !client.OidcEnabled)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidClient);
            }

            if (!ValidOpaqueRefreshToken(request.RefreshToken))
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            AuthenticationDirectoryLocation location;

            try
            {
                location = await directoryLocator
                    .LocateAsync(
                        client.Application,
                        client.AuthenticationContextKey,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.DirectoryUnavailable);
            }

            var now =
                timeProvider.GetUtcNow();

            var issuedRefreshToken =
                refreshTokenService.Issue();

            OidcRefreshTokenRotationResult rotation;

            try
            {
                rotation = await refreshTokens
                    .RotateAsync(
                        location.Route,
                        client.ClientId,
                        refreshTokenService.Hash(request.RefreshToken!),
                        Guid.NewGuid(),
                        issuedRefreshToken.Hash,
                        now,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            if (rotation.Status == OidcRefreshTokenRotationStatus.ReuseDetected)
            {
                await auditWriter
                    .TryWriteAsync(
                        location.Route,
                        new SecurityAuditEvent(
                            SecurityAuditEventType.OidcRefreshTokenReuseDetected,
                            SecurityAuditOutcome.Denied,
                            location.Route.Request.IdentityScopeId,
                            application: client.Application,
                            clientId: client.ClientId),
                        cancellationToken)
                    .ConfigureAwait(false);

                await auditWriter
                    .TryWriteAsync(
                        location.Route,
                        new SecurityAuditEvent(
                            SecurityAuditEventType.OidcRefreshTokenFamilyRevoked,
                            SecurityAuditOutcome.Succeeded,
                            location.Route.Request.IdentityScopeId,
                            application: client.Application,
                            clientId: client.ClientId),
                        cancellationToken)
                    .ConfigureAwait(false);

                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            if (rotation.Status != OidcRefreshTokenRotationStatus.Rotated ||
                rotation.Grant is null)
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.InvalidGrant);
            }

            var grant =
                rotation.Grant;

            OidcIssuedAccessToken accessToken;

            try
            {
                accessToken = tokenIssuer.IssueAccessToken(
                    new OidcAccessTokenIssueRequest(
                        grant.Subject,
                        grant.SessionId,
                        grant.ClientId,
                        grant.Application,
                        grant.Scope,
                        now));
            }
            catch
            {
                return OidcTokenResult.Failure(
                    OidcTokenFailureCode.TokenIssuanceFailed);
            }

            await auditWriter
                .TryWriteAsync(
                    location.Route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.OidcRefreshTokenRotated,
                        SecurityAuditOutcome.Succeeded,
                        grant.Subject.IdentityScopeId,
                        userId: grant.Subject.UserId,
                        application: grant.Application,
                        clientId: grant.ClientId,
                        targetId: grant.TokenId.ToString("D")),
                    cancellationToken)
                .ConfigureAwait(false);

            return OidcTokenResult.RefreshSuccess(
                accessToken,
                issuedRefreshToken.Value,
                grant.Scope);
        }

        private static bool ValidOpaqueRefreshToken(
            string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Length == 43 &&
            value.All(
                character =>
                    char.IsAsciiLetterOrDigit(character) ||
                    character is '-' or '_');

        private static bool ValidOpaqueState(
            string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Length is >= 8 and <= 512 &&
            !value.Any(char.IsControl);

        private static bool ValidNonce(
            string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Length is >= 8 and <= 256 &&
            !value.Any(char.IsControl);
    }
}
