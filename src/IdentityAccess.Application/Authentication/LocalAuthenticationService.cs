using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Provides application operations for local authentication.</summary>
    public sealed class LocalAuthenticationService(
        IAuthenticationClientRegistry clients,
        IAuthenticationDirectoryLocator directoryLocator,
        IPasswordCredentialStore credentials,
        IAuthenticationSessionStore sessions,
        IPasswordHashingService passwordHasher,
        ISessionTokenService sessionTokens,
        AuthenticationOptions options,
        TimeProvider timeProvider,
        ISecurityAuditWriter auditWriter) : ILocalAuthenticationService
    {
        /// <summary>Authenticates a local password credential and issues an opaque session on success.</summary>
        public async Task<PasswordLoginResult> LoginAsync(string clientId, string loginIdentifier, string password,
            string redirectUri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!clients.TryGet(clientId, out var client))
                return new PasswordLoginResult(PasswordLoginDecision.ClientRejected, FailureCode: AuthenticationFailureCode.UnknownClient);
            if (!client.AllowsRedirectUri(redirectUri))
                return new PasswordLoginResult(PasswordLoginDecision.RedirectRejected, FailureCode: AuthenticationFailureCode.RedirectUriRejected);

            AuthenticationDirectoryLocation location;
            try
            {
                location = await directoryLocator.LocateAsync(client.Application, client.AuthenticationContextKey,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return new PasswordLoginResult(PasswordLoginDecision.Unavailable, FailureCode: AuthenticationFailureCode.DirectoryUnavailable);
            }

            LoginIdentifier login;
            try { login = new LoginIdentifier(loginIdentifier); }
            catch (ArgumentException)
            {
                passwordHasher.ConsumeUnknownCredential(password);
                await AuditLoginFailureAsync(location.Route, client, null, cancellationToken)
                    .ConfigureAwait(false);
                return InvalidCredentials();
            }

            var credential = await credentials.FindByLoginAsync(location.Route, login.NormalizedValue,
                cancellationToken).ConfigureAwait(false);
            if (credential is null)
            {
                passwordHasher.ConsumeUnknownCredential(password);
                await AuditLoginFailureAsync(location.Route, client, null, cancellationToken)
                    .ConfigureAwait(false);
                return InvalidCredentials();
            }

            var now = timeProvider.GetUtcNow();
            var verification = passwordHasher.Verify(credential.Value.Subject, credential.Value.PasswordHash, password);
            if (credential.Value.LockoutUntil is { } lockedUntil && lockedUntil > now)
            {
                await AuditLoginFailureAsync(
                    location.Route,
                    client,
                    credential.Value.Subject.UserId,
                    cancellationToken).ConfigureAwait(false);
                return InvalidCredentials();
            }

            if (verification == PasswordHashVerification.Failed)
            {
                await credentials.RecordFailureAsync(location.Route, credential.Value.Subject,
                    options.LockoutAttempts, now.AddMinutes(options.LockoutMinutes), cancellationToken)
                    .ConfigureAwait(false);
                await AuditLoginFailureAsync(
                    location.Route,
                    client,
                    credential.Value.Subject.UserId,
                    cancellationToken).ConfigureAwait(false);
                return InvalidCredentials();
            }

            await credentials.RecordSuccessAsync(location.Route, credential.Value.Subject, cancellationToken)
                .ConfigureAwait(false);

            var issued = sessionTokens.Issue();
            var sessionId = Guid.NewGuid();
            var expiresAt = now.AddMinutes(options.SessionLifetimeMinutes);
            var session = new AuthenticationSession(sessionId, credential.Value.Subject, client.ClientId,
                client.Application, client.AuthenticationContextKey, now, expiresAt);
            var created = await sessions.CreateForActiveUserAsync(
                location.Route,
                session,
                issued.Hash,
                cancellationToken).ConfigureAwait(false);

            if (!created)
            {
                await AuditLoginFailureAsync(
                    location.Route,
                    client,
                    credential.Value.Subject.UserId,
                    cancellationToken).ConfigureAwait(false);
                return InvalidCredentials();
            }

            await auditWriter.TryWriteAsync(
                location.Route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.PasswordLoginSucceeded,
                    SecurityAuditOutcome.Succeeded,
                    location.Route.Request.IdentityScopeId,
                    userId: credential.Value.Subject.UserId,
                    application: client.Application,
                    clientId: client.ClientId,
                    targetId: sessionId.ToString("D")),
                cancellationToken).ConfigureAwait(false);

            return new PasswordLoginResult(PasswordLoginDecision.Succeeded, credential.Value.Subject.UserId,
                sessionId, issued.Value, expiresAt, redirectUri);
        }

        /// <summary>Validates an opaque local authentication session.</summary>
        public async Task<SessionValidationResult> ValidateSessionAsync(string clientId, Guid sessionId,
            string sessionToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!clients.TryGet(clientId, out var client) || sessionId == Guid.Empty || string.IsNullOrWhiteSpace(sessionToken))
                return new SessionValidationResult(false);

            AuthenticationDirectoryLocation location;
            try
            {
                location = await directoryLocator.LocateAsync(client.Application, client.AuthenticationContextKey,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return new SessionValidationResult(false);
            }

            var session = await sessions.ValidateAsync(location.Route, clientId, sessionId,
                sessionTokens.Hash(sessionToken), timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
            if (session is null || session.Application != client.Application ||
                !string.Equals(session.AuthenticationContextKey, client.AuthenticationContextKey, StringComparison.Ordinal))
                return new SessionValidationResult(false);
            return new SessionValidationResult(
                true,
                new AuthenticatedSessionContext(
                    session.Subject,
                    session.SessionId,
                    session.ClientId,
                    session.Application,
                    session.AuthenticationContextKey,
                    session.CreatedAt,
                    session.ExpiresAt));
        }

        /// <summary>Revokes an opaque local session and validates an optional post-logout redirect URI.</summary>
        public async Task<LogoutResult> LogoutAsync(string clientId, Guid sessionId, string sessionToken,
            string? postLogoutRedirectUri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!clients.TryGet(clientId, out var client))
                return new LogoutResult(false, FailureCode: AuthenticationFailureCode.UnknownClient);
            if (postLogoutRedirectUri is not null && !client.AllowsPostLogoutRedirectUri(postLogoutRedirectUri))
                return new LogoutResult(false, FailureCode: AuthenticationFailureCode.PostLogoutRedirectUriRejected);
            if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(sessionToken))
                return new LogoutResult(false, postLogoutRedirectUri, AuthenticationFailureCode.InvalidSession);

            AuthenticationDirectoryLocation location;
            try
            {
                location = await directoryLocator.LocateAsync(client.Application, client.AuthenticationContextKey,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return new LogoutResult(false, postLogoutRedirectUri, AuthenticationFailureCode.DirectoryUnavailable);
            }

            var revoked = await sessions.RevokeAsync(location.Route, clientId, sessionId,
                sessionTokens.Hash(sessionToken), timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);

            await auditWriter.TryWriteAsync(
                location.Route,
                new SecurityAuditEvent(
                    revoked
                        ? SecurityAuditEventType.SessionRevoked
                        : SecurityAuditEventType.SessionRevocationFailed,
                    revoked
                        ? SecurityAuditOutcome.Succeeded
                        : SecurityAuditOutcome.Denied,
                    location.Route.Request.IdentityScopeId,
                    application: client.Application,
                    clientId: client.ClientId,
                    targetId: sessionId.ToString("D"),
                    reasonCode: revoked
                        ? null
                        : SecurityAuditReasonCode.InvalidSession),
                cancellationToken).ConfigureAwait(false);

            return new LogoutResult(revoked, postLogoutRedirectUri);
        }

        private Task<bool> AuditLoginFailureAsync(
            ResolvedDatabaseRoute route,
            AuthenticationClientRegistration client,
            Guid? userId,
            CancellationToken cancellationToken) =>
            auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.PasswordLoginFailed,
                    SecurityAuditOutcome.Denied,
                    route.Request.IdentityScopeId,
                    userId: userId,
                    application: client.Application,
                    clientId: client.ClientId,
                    reasonCode: SecurityAuditReasonCode.InvalidCredentials),
                cancellationToken);

        private static PasswordLoginResult InvalidCredentials() =>
            new(PasswordLoginDecision.InvalidCredentials, FailureCode: AuthenticationFailureCode.InvalidCredentials);
    }
}
