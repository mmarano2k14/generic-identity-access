using System.Security.Cryptography;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Coordinates WebAuthn registration challenges, protocol validation, persistence, and audit.</summary>
    internal sealed class WebAuthnRegistrationService : IWebAuthnRegistrationService
    {
        private readonly IDatabaseRouteResolver _routeResolver;
        private readonly IWebAuthnCredentialStore _store;
        private readonly ISecurityAuditWriter _auditWriter;
        private readonly TimeProvider _timeProvider;
        private readonly WebAuthnProviderOptions _options;

        public WebAuthnRegistrationService(
            IDatabaseRouteResolver routeResolver,
            IWebAuthnCredentialStore store,
            ISecurityAuditWriter auditWriter,
            TimeProvider timeProvider,
            WebAuthnProviderOptions options)
        {
            ArgumentNullException.ThrowIfNull(routeResolver);
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(auditWriter);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(options);

            _routeResolver = routeResolver;
            _store = store;
            _auditWriter = auditWriter;
            _timeProvider = timeProvider;
            _options = options;
        }

        public async Task<WebAuthnRegistrationOptions> BeginRegistrationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            string displayName,
            string userName,
            string userDisplayName,
            CancellationToken cancellationToken)
        {
            ValidateContext(identityScopeId, application, userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ValidateUserText(userName, nameof(userName));
            ValidateUserText(userDisplayName, nameof(userDisplayName));

            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var existingCredentialIds = await _store.ListActiveCredentialIdsAsync(
                route,
                identityScopeId,
                userId,
                cancellationToken).ConfigureAwait(false);

            var authenticatorId = Guid.NewGuid();
            var createdAt = _timeProvider.GetUtcNow();
            var expiresAt = createdAt.AddSeconds(_options.ChallengeLifetimeSeconds);
            var challenge = RandomNumberGenerator.GetBytes(WebAuthnProviderOptions.ChallengeLengthBytes);
            var challengeHash = SHA256.HashData(challenge);

            try
            {
                var authenticator = new UserAuthenticator(
                    identityScopeId,
                    authenticatorId,
                    userId,
                    WebAuthnAuthenticationFactorProviderKey.Instance,
                    displayName,
                    UserAuthenticatorStatus.Pending,
                    createdAt,
                    confirmedAt: null,
                    lastUsedAt: null,
                    revokedAt: null);

                await _store.CreatePendingRegistrationAsync(
                    route,
                    authenticator,
                    application,
                    challengeHash,
                    expiresAt,
                    cancellationToken).ConfigureAwait(false);

                await AuditAsync(
                    route,
                    SecurityAuditEventType.UserAuthenticatorEnrollmentStarted,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    reasonCode: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var userHandle = DeriveUserHandle(identityScopeId, userId);
                try
                {
                    return new WebAuthnRegistrationOptions(
                        authenticatorId,
                        WebAuthnBase64Url.Encode(challenge),
                        _options.RelyingPartyId,
                        _options.RelyingPartyName,
                        WebAuthnBase64Url.Encode(userHandle),
                        userName,
                        userDisplayName,
                        _options.ChallengeLifetimeSeconds * 1000,
                        [WebAuthnProviderOptions.CoseAlgorithmEs256],
                        existingCredentialIds.Select(value => WebAuthnBase64Url.Encode(value)));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(userHandle);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(challenge);
                CryptographicOperations.ZeroMemory(challengeHash);
            }
        }

        public async Task<WebAuthnRegistrationResult> CompleteRegistrationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            WebAuthnRegistrationResponse response,
            CancellationToken cancellationToken)
        {
            ValidateContext(identityScopeId, application, userId);
            if (authenticatorId == Guid.Empty) throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            ArgumentNullException.ThrowIfNull(response);

            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var state = await _store.GetPendingRegistrationAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                cancellationToken).ConfigureAwait(false);

            if (state is null) return WebAuthnRegistrationResult.NotFound;
            if (state.ConsumedAt is not null)
            {
                await AuditAsync(
                    route,
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    SecurityAuditOutcome.Denied,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.AuthenticationFactorReplayDetected,
                    cancellationToken).ConfigureAwait(false);
                return WebAuthnRegistrationResult.AlreadyUsed;
            }
            if (state.Status != UserAuthenticatorStatus.Pending ||
                !string.Equals(state.Application.Value, application.Value, StringComparison.Ordinal))
            {
                return WebAuthnRegistrationResult.InvalidState;
            }

            var now = _timeProvider.GetUtcNow();
            if (now > state.ExpiresAt)
            {
                await AuditAsync(
                    route,
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    SecurityAuditOutcome.Denied,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return WebAuthnRegistrationResult.Expired;
            }

            var userHandle = DeriveUserHandle(identityScopeId, userId);
            WebAuthnRegistrationVerificationResult verification;
            try
            {
                verification = WebAuthnRegistrationVerifier.Verify(state, response, _options, userHandle);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(userHandle);
            }

            if (!verification.Succeeded)
            {
                await AuditAsync(
                    route,
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    SecurityAuditOutcome.Denied,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return verification.ClientDataValid
                    ? WebAuthnRegistrationResult.InvalidAttestation
                    : WebAuthnRegistrationResult.InvalidClientData;
            }

            var storeResult = await _store.TryCompleteRegistrationAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                application,
                verification.Credential!,
                now,
                cancellationToken).ConfigureAwait(false);

            if (storeResult == WebAuthnRegistrationStoreResult.Succeeded)
            {
                await AuditAsync(
                    route,
                    SecurityAuditEventType.UserAuthenticatorEnrollmentConfirmed,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    reasonCode: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return WebAuthnRegistrationResult.Registered;
            }

            return storeResult switch
            {
                WebAuthnRegistrationStoreResult.NotFound => WebAuthnRegistrationResult.NotFound,
                WebAuthnRegistrationStoreResult.Expired => WebAuthnRegistrationResult.Expired,
                WebAuthnRegistrationStoreResult.AlreadyUsed => WebAuthnRegistrationResult.AlreadyUsed,
                WebAuthnRegistrationStoreResult.CredentialAlreadyRegistered => WebAuthnRegistrationResult.CredentialAlreadyRegistered,
                _ => WebAuthnRegistrationResult.InvalidState
            };
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken) =>
            _routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId, IdentityDataSet.IdentityDirectory),
                cancellationToken);

        private Task<bool> AuditAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEventType eventType,
            SecurityAuditOutcome outcome,
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            SecurityAuditReasonCode? reasonCode,
            CancellationToken cancellationToken) =>
            _auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    eventType,
                    outcome,
                    identityScopeId,
                    userId: userId,
                    application: application,
                    targetId: authenticatorId.ToString("D"),
                    reasonCode: reasonCode),
                cancellationToken);

        private static byte[] DeriveUserHandle(Guid identityScopeId, Guid userId)
        {
            Span<byte> input = stackalloc byte[32];
            identityScopeId.TryWriteBytes(input[..16]);
            userId.TryWriteBytes(input[16..]);
            return SHA256.HashData(input);
        }

        private static void ValidateContext(Guid identityScopeId, ApplicationKey application, Guid userId)
        {
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
        }

        private static void ValidateUserText(string value, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            if (value.Trim().Length > 256)
                throw new ArgumentException("WebAuthn user text must not exceed 256 characters.", parameterName);
        }
    }
}
