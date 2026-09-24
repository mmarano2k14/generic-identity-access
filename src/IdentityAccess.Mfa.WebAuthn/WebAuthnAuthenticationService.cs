using System.Security.Cryptography;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Coordinates WebAuthn authentication challenges, assertion validation, persistence, and audit.</summary>
    internal sealed class WebAuthnAuthenticationService : IWebAuthnAuthenticationService
    {
        private readonly IDatabaseRouteResolver _routeResolver;
        private readonly IWebAuthnCredentialStore _store;
        private readonly ISecurityAuditWriter _auditWriter;
        private readonly IMfaProviderPolicyGuard _policyGuard;
        private readonly TimeProvider _timeProvider;
        private readonly WebAuthnProviderOptions _options;

        public WebAuthnAuthenticationService(
            IDatabaseRouteResolver routeResolver,
            IWebAuthnCredentialStore store,
            ISecurityAuditWriter auditWriter,
            IMfaProviderPolicyGuard policyGuard,
            TimeProvider timeProvider,
            WebAuthnProviderOptions options)
        {
            ArgumentNullException.ThrowIfNull(routeResolver);
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(auditWriter);
            ArgumentNullException.ThrowIfNull(policyGuard);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(options);

            _routeResolver = routeResolver;
            _store = store;
            _auditWriter = auditWriter;
            _policyGuard = policyGuard;
            _timeProvider = timeProvider;
            _options = options;
        }

        public async Task<WebAuthnAuthenticationOptions> BeginAuthenticationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken)
        {
            ValidateContext(identityScopeId, application, userId);

            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            await EnsurePolicyAllowedAsync(route, identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var credentials = await _store.ListActiveCredentialsAsync(
                route,
                identityScopeId,
                userId,
                cancellationToken).ConfigureAwait(false);

            var challengeId = Guid.NewGuid();
            var createdAt = _timeProvider.GetUtcNow();
            var expiresAt = createdAt.AddSeconds(_options.ChallengeLifetimeSeconds);
            var challenge = RandomNumberGenerator.GetBytes(WebAuthnProviderOptions.ChallengeLengthBytes);
            var challengeHash = SHA256.HashData(challenge);

            try
            {
                await _store.CreateAuthenticationChallengeAsync(
                    route,
                    identityScopeId,
                    userId,
                    challengeId,
                    application,
                    challengeHash,
                    createdAt,
                    expiresAt,
                    cancellationToken).ConfigureAwait(false);

                return new WebAuthnAuthenticationOptions(
                    challengeId,
                    WebAuthnBase64Url.Encode(challenge),
                    _options.RelyingPartyId,
                    _options.ChallengeLifetimeSeconds * 1000,
                    credentials.Select(value => WebAuthnBase64Url.Encode(value.CredentialId)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(challenge);
                CryptographicOperations.ZeroMemory(challengeHash);
            }
        }

        public async Task<WebAuthnAuthenticationResult> CompleteAuthenticationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid challengeId,
            WebAuthnAuthenticationResponse response,
            CancellationToken cancellationToken)
        {
            ValidateContext(identityScopeId, application, userId);
            if (challengeId == Guid.Empty) throw new ArgumentException("Challenge identifier is required.", nameof(challengeId));
            ArgumentNullException.ThrowIfNull(response);

            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            await EnsurePolicyAllowedAsync(route, identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var challengeState = await _store.GetAuthenticationChallengeAsync(
                route,
                identityScopeId,
                userId,
                challengeId,
                cancellationToken).ConfigureAwait(false);

            if (challengeState is null) return WebAuthnAuthenticationResult.NotFound;
            if (challengeState.ConsumedAt is not null)
            {
                await AuditFailureAsync(
                    route,
                    identityScopeId,
                    application,
                    userId,
                    targetId: challengeId.ToString("D"),
                    SecurityAuditReasonCode.AuthenticationFactorReplayDetected,
                    cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationResult.AlreadyUsed;
            }

            if (!string.Equals(challengeState.Application.Value, application.Value, StringComparison.Ordinal))
                return WebAuthnAuthenticationResult.InvalidState;

            var now = _timeProvider.GetUtcNow();
            if (now > challengeState.ExpiresAt)
            {
                await AuditFailureAsync(
                    route,
                    identityScopeId,
                    application,
                    userId,
                    targetId: challengeId.ToString("D"),
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationResult.Expired;
            }

            if (!WebAuthnBase64Url.TryDecode(response.CredentialId, 1023, out var credentialId))
            {
                await AuditFailureAsync(
                    route,
                    identityScopeId,
                    application,
                    userId,
                    targetId: challengeId.ToString("D"),
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationResult.InvalidClientData;
            }

            WebAuthnCredentialRecord? credential;
            try
            {
                credential = await _store.GetActiveCredentialAsync(
                    route,
                    identityScopeId,
                    userId,
                    credentialId,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(credentialId);
            }

            if (credential is null) return WebAuthnAuthenticationResult.NotFound;

            var verification = WebAuthnAssertionVerifier.Verify(
                challengeState,
                credential,
                response,
                _options);

            if (!verification.Succeeded)
            {
                await AuditFailureAsync(
                    route,
                    identityScopeId,
                    application,
                    userId,
                    credential.AuthenticatorId.ToString("D"),
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return verification.ClientDataValid
                    ? WebAuthnAuthenticationResult.InvalidAssertion
                    : WebAuthnAuthenticationResult.InvalidClientData;
            }

            var storeResult = await _store.TryCompleteAuthenticationAsync(
                route,
                identityScopeId,
                userId,
                challengeId,
                application,
                credential.CredentialId,
                verification.SignCount,
                verification.BackupEligible,
                verification.BackupState,
                now,
                cancellationToken).ConfigureAwait(false);

            if (storeResult == WebAuthnAuthenticationStoreResult.Succeeded)
            {
                await _auditWriter.TryWriteAsync(
                    route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.AuthenticationFactorVerificationSucceeded,
                        SecurityAuditOutcome.Succeeded,
                        identityScopeId,
                        userId: userId,
                        application: application,
                        targetId: credential.AuthenticatorId.ToString("D")),
                    cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationResult.Succeeded;
            }

            if (storeResult is WebAuthnAuthenticationStoreResult.ReplayDetected or WebAuthnAuthenticationStoreResult.AlreadyUsed)
            {
                await AuditFailureAsync(
                    route,
                    identityScopeId,
                    application,
                    userId,
                    credential.AuthenticatorId.ToString("D"),
                    SecurityAuditReasonCode.AuthenticationFactorReplayDetected,
                    cancellationToken).ConfigureAwait(false);
            }

            return storeResult switch
            {
                WebAuthnAuthenticationStoreResult.NotFound => WebAuthnAuthenticationResult.NotFound,
                WebAuthnAuthenticationStoreResult.Expired => WebAuthnAuthenticationResult.Expired,
                WebAuthnAuthenticationStoreResult.AlreadyUsed => WebAuthnAuthenticationResult.AlreadyUsed,
                WebAuthnAuthenticationStoreResult.ReplayDetected => WebAuthnAuthenticationResult.ReplayDetected,
                _ => WebAuthnAuthenticationResult.InvalidState
            };
        }

        private async Task EnsurePolicyAllowedAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            var decision = await _policyGuard
                .EvaluateAsync(
                    route,
                    identityScopeId,
                    application,
                    WebAuthnAuthenticationFactorProviderKey.Instance,
                    cancellationToken)
                .ConfigureAwait(false);

            if (decision != MfaProviderPolicyDecision.Allowed)
                throw new MfaProviderPolicyException(decision);
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken) =>
            _routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId, IdentityDataSet.IdentityDirectory),
                cancellationToken);

        private Task<bool> AuditFailureAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            string targetId,
            SecurityAuditReasonCode reasonCode,
            CancellationToken cancellationToken) =>
            _auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    SecurityAuditOutcome.Denied,
                    identityScopeId,
                    userId: userId,
                    application: application,
                    targetId: targetId,
                    reasonCode: reasonCode),
                cancellationToken);

        private static void ValidateContext(Guid identityScopeId, ApplicationKey application, Guid userId)
        {
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
        }
    }
}
