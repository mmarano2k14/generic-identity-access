using System.Security.Cryptography;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Coordinates recovery-code generation, replacement, consumption, audit, and routing.</summary>
    internal sealed class RecoveryAuthenticationFactorService : IRecoveryAuthenticationFactorService
    {
        private readonly IDatabaseRouteResolver _routeResolver;
        private readonly IRecoveryCodeStore _store;
        private readonly ISecurityAuditWriter _auditWriter;
        private readonly IMfaProviderPolicyGuard _policyGuard;
        private readonly TimeProvider _timeProvider;
        private readonly RecoveryCodeProviderOptions _options;

        public RecoveryAuthenticationFactorService(
            IDatabaseRouteResolver routeResolver,
            IRecoveryCodeStore store,
            ISecurityAuditWriter auditWriter,
            IMfaProviderPolicyGuard policyGuard,
            TimeProvider timeProvider,
            RecoveryCodeProviderOptions options)
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

        public async Task<RecoveryCodeSet> GenerateOrReplaceAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            string displayName,
            CancellationToken cancellationToken)
        {
            ValidateContext(identityScopeId, application, userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            await EnsurePolicyAllowedAsync(route, identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var authenticatorId = Guid.NewGuid();
            var createdAt = _timeProvider.GetUtcNow();
            var codes = RecoveryCodeGenerator.GenerateSet(_options.CodeCount);
            var hashes = new List<byte[]>(codes.Length);

            try
            {
                foreach (var code in codes)
                {
                    if (!RecoveryCodeGenerator.TryHash(code, out var hash))
                        throw new InvalidOperationException("Generated recovery code could not be canonicalized.");
                    hashes.Add(hash);
                }

                var authenticator = new UserAuthenticator(
                    identityScopeId,
                    authenticatorId,
                    userId,
                    RecoveryAuthenticationFactorProviderKey.Instance,
                    displayName,
                    UserAuthenticatorStatus.Active,
                    createdAt,
                    confirmedAt: createdAt,
                    lastUsedAt: null,
                    revokedAt: null);

                var replaced = await _store.ReplaceActiveSetAsync(
                    route,
                    authenticator,
                    hashes,
                    cancellationToken).ConfigureAwait(false);

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

                return new RecoveryCodeSet(authenticatorId, codes, replaced);
            }
            finally
            {
                foreach (var hash in hashes)
                {
                    CryptographicOperations.ZeroMemory(hash);
                }
            }
        }

        public async Task<RecoveryCodeVerificationResult> VerifyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            string code,
            CancellationToken cancellationToken)
        {
            ValidateContext(identityScopeId, application, userId);
            ValidateAuthenticatorId(authenticatorId);

            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            await EnsurePolicyAllowedAsync(route, identityScopeId, application, cancellationToken).ConfigureAwait(false);
            if (!RecoveryCodeGenerator.TryHash(code, out var hash))
            {
                await AuditDeniedAsync(
                    route,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return RecoveryCodeVerificationResult.InvalidCode;
            }

            try
            {
                var result = await _store.TryConsumeAsync(
                    route,
                    identityScopeId,
                    userId,
                    authenticatorId,
                    hash,
                    _timeProvider.GetUtcNow(),
                    cancellationToken).ConfigureAwait(false);

                if (result == RecoveryCodeStoreMutationResult.Succeeded)
                {
                    await AuditAsync(
                        route,
                        SecurityAuditEventType.AuthenticationFactorVerificationSucceeded,
                        SecurityAuditOutcome.Succeeded,
                        identityScopeId,
                        application,
                        userId,
                        authenticatorId,
                        reasonCode: null,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    return RecoveryCodeVerificationResult.Succeeded;
                }

                if (result == RecoveryCodeStoreMutationResult.AlreadyConsumed)
                {
                    await AuditDeniedAsync(
                        route,
                        identityScopeId,
                        application,
                        userId,
                        authenticatorId,
                        SecurityAuditReasonCode.AuthenticationFactorReplayDetected,
                        cancellationToken).ConfigureAwait(false);
                    return RecoveryCodeVerificationResult.AlreadyConsumed;
                }

                if (result == RecoveryCodeStoreMutationResult.InvalidCode)
                {
                    await AuditDeniedAsync(
                        route,
                        identityScopeId,
                        application,
                        userId,
                        authenticatorId,
                        SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                        cancellationToken).ConfigureAwait(false);
                    return RecoveryCodeVerificationResult.InvalidCode;
                }

                return result == RecoveryCodeStoreMutationResult.NotFound
                    ? RecoveryCodeVerificationResult.NotFound
                    : RecoveryCodeVerificationResult.NotActive;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hash);
            }
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
                    RecoveryAuthenticationFactorProviderKey.Instance,
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
                new DatabaseRouteRequest(
                    application,
                    identityScopeId,
                    IdentityDataSet.IdentityDirectory),
                cancellationToken);

        private Task<bool> AuditDeniedAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            SecurityAuditReasonCode reasonCode,
            CancellationToken cancellationToken) =>
            AuditAsync(
                route,
                SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                SecurityAuditOutcome.Denied,
                identityScopeId,
                application,
                userId,
                authenticatorId,
                reasonCode,
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

        private static void ValidateContext(Guid identityScopeId, ApplicationKey application, Guid userId)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            if (userId == Guid.Empty)
                throw new ArgumentException("User identifier is required.", nameof(userId));
        }

        private static void ValidateAuthenticatorId(Guid authenticatorId)
        {
            if (authenticatorId == Guid.Empty)
                throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
        }
    }
}
