using System.Security.Cryptography;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Coordinates provider-owned TOTP enrollment, confirmation, verification, audit, and routing.</summary>
    internal sealed class TotpAuthenticationFactorService : ITotpAuthenticationFactorService
    {
        private readonly IDatabaseRouteResolver _routeResolver;
        private readonly ITotpAuthenticatorStore _store;
        private readonly ITotpSecretProtector _secretProtector;
        private readonly ISecurityAuditWriter _auditWriter;
        private readonly TimeProvider _timeProvider;
        private readonly TotpProviderOptions _options;

        public TotpAuthenticationFactorService(
            IDatabaseRouteResolver routeResolver,
            ITotpAuthenticatorStore store,
            ITotpSecretProtector secretProtector,
            ISecurityAuditWriter auditWriter,
            TimeProvider timeProvider,
            TotpProviderOptions options)
        {
            ArgumentNullException.ThrowIfNull(routeResolver);
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(secretProtector);
            ArgumentNullException.ThrowIfNull(auditWriter);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(options);

            _routeResolver = routeResolver;
            _store = store;
            _secretProtector = secretProtector;
            _auditWriter = auditWriter;
            _timeProvider = timeProvider;
            _options = options;
        }

        public async Task<TotpEnrollment> BeginEnrollmentAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            string displayName,
            string accountName,
            CancellationToken cancellationToken)
        {
            ValidateContext(identityScopeId, application, userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
            if (accountName.Trim().Length > 256)
                throw new ArgumentException("TOTP account name must not exceed 256 characters.", nameof(accountName));

            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var authenticatorId = Guid.NewGuid();
            var createdAt = _timeProvider.GetUtcNow();
            var secret = RandomNumberGenerator.GetBytes(TotpProviderOptions.SecretLengthBytes);

            try
            {
                var protectedSecret = _secretProtector.Protect(secret);
                var authenticator = new UserAuthenticator(
                    identityScopeId,
                    authenticatorId,
                    userId,
                    TotpAuthenticationFactorProviderKey.Instance,
                    displayName,
                    UserAuthenticatorStatus.Pending,
                    createdAt,
                    confirmedAt: null,
                    lastUsedAt: null,
                    revokedAt: null);

                await _store.CreatePendingAsync(
                    route,
                    authenticator,
                    protectedSecret,
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

                var base32Secret = TotpBase32.Encode(secret);
                var provisioningUri = BuildProvisioningUri(base32Secret, accountName.Trim());
                return new TotpEnrollment(
                    authenticatorId,
                    base32Secret,
                    provisioningUri,
                    TotpProviderOptions.AlgorithmName,
                    TotpProviderOptions.Digits,
                    TotpProviderOptions.PeriodSeconds);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }
        }

        public async Task<TotpConfirmationResult> ConfirmEnrollmentAsync(
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
            var state = await _store.GetAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                cancellationToken).ConfigureAwait(false);

            if (state is null)
                return TotpConfirmationResult.NotFound;
            if (state.Status != UserAuthenticatorStatus.Pending)
                return TotpConfirmationResult.InvalidState;

            var now = _timeProvider.GetUtcNow();
            if (!TryValidate(state, code, now, out var acceptedTimeStep))
            {
                await AuditDeniedAsync(
                    route,
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return TotpConfirmationResult.InvalidCode;
            }

            var result = await _store.TryConfirmAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                acceptedTimeStep,
                now,
                cancellationToken).ConfigureAwait(false);

            if (result == TotpStoreMutationResult.Succeeded)
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
                return TotpConfirmationResult.Confirmed;
            }

            if (result == TotpStoreMutationResult.ReplayDetected)
            {
                await AuditDeniedAsync(
                    route,
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.AuthenticationFactorReplayDetected,
                    cancellationToken).ConfigureAwait(false);
                return TotpConfirmationResult.ReplayDetected;
            }

            return result == TotpStoreMutationResult.NotFound
                ? TotpConfirmationResult.NotFound
                : TotpConfirmationResult.InvalidState;
        }

        public async Task<TotpVerificationResult> VerifyAsync(
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
            var state = await _store.GetAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                cancellationToken).ConfigureAwait(false);

            if (state is null)
                return TotpVerificationResult.NotFound;
            if (state.Status != UserAuthenticatorStatus.Active)
                return TotpVerificationResult.NotActive;

            var now = _timeProvider.GetUtcNow();
            if (!TryValidate(state, code, now, out var acceptedTimeStep))
            {
                await AuditDeniedAsync(
                    route,
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.InvalidAuthenticationFactorProof,
                    cancellationToken).ConfigureAwait(false);
                return TotpVerificationResult.InvalidCode;
            }

            var result = await _store.TryRecordSuccessfulVerificationAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                acceptedTimeStep,
                now,
                cancellationToken).ConfigureAwait(false);

            if (result == TotpStoreMutationResult.Succeeded)
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
                return TotpVerificationResult.Succeeded;
            }

            if (result == TotpStoreMutationResult.ReplayDetected)
            {
                await AuditDeniedAsync(
                    route,
                    SecurityAuditEventType.AuthenticationFactorVerificationFailed,
                    identityScopeId,
                    application,
                    userId,
                    authenticatorId,
                    SecurityAuditReasonCode.AuthenticationFactorReplayDetected,
                    cancellationToken).ConfigureAwait(false);
                return TotpVerificationResult.ReplayDetected;
            }

            return result == TotpStoreMutationResult.NotFound
                ? TotpVerificationResult.NotFound
                : TotpVerificationResult.NotActive;
        }

        private bool TryValidate(
            TotpAuthenticatorState state,
            string code,
            DateTimeOffset now,
            out long acceptedTimeStep)
        {
            acceptedTimeStep = -1;
            if (!string.Equals(state.Algorithm, TotpProviderOptions.AlgorithmName, StringComparison.Ordinal) ||
                state.Digits != TotpProviderOptions.Digits ||
                state.PeriodSeconds != TotpProviderOptions.PeriodSeconds)
            {
                throw new InvalidOperationException("Stored TOTP parameters are not supported by this provider version.");
            }

            var secret = _secretProtector.Unprotect(state.ProtectedSecret);
            try
            {
                return TotpCodeGenerator.TryValidate(
                    secret,
                    code,
                    now.ToUnixTimeSeconds(),
                    state.Digits,
                    state.PeriodSeconds,
                    _options.AllowedClockSkewSteps,
                    out acceptedTimeStep);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secret);
            }
        }

        private string BuildProvisioningUri(string base32Secret, string accountName)
        {
            var issuer = Uri.EscapeDataString(_options.Issuer);
            var account = Uri.EscapeDataString(accountName);
            var label = $"{issuer}:{account}";
            return $"otpauth://totp/{label}?secret={base32Secret}&issuer={issuer}&algorithm={TotpProviderOptions.AlgorithmName}&digits={TotpProviderOptions.Digits}&period={TotpProviderOptions.PeriodSeconds}";
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
            SecurityAuditEventType eventType,
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            SecurityAuditReasonCode reasonCode,
            CancellationToken cancellationToken) =>
            AuditAsync(
                route,
                eventType,
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
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
        }

        private static void ValidateAuthenticatorId(Guid authenticatorId)
        {
            if (authenticatorId == Guid.Empty)
                throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
        }
    }
}
