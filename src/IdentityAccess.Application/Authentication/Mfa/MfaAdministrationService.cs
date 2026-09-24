using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Generic provider-neutral MFA administration service.</summary>
    public sealed class MfaAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IMfaPolicyStore policies,
        IUserAuthenticatorStore authenticators,
        IAuthenticationFactorProviderRegistry providers,
        ISessionAdministrationService sessionAdministration,
        ISecurityAuditWriter auditWriter,
        TimeProvider timeProvider) : IMfaAdministrationService
    {
        /// <inheritdoc />
        public IReadOnlyList<AuthenticationFactorProviderDescriptor> ListProviders() => providers.List();

        /// <inheritdoc />
        public async Task<VersionedRecord<MfaPolicy>?> GetPolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await policies.GetAsync(route, identityScopeId, application, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<MfaPolicy>> CreatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            MfaPolicyMode mode,
            IReadOnlyCollection<AuthenticationFactorProviderKey> allowedProviders,
            CancellationToken cancellationToken)
        {
            EnsureProvidersRegistered(allowedProviders);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policy = new MfaPolicy(identityScopeId, application, mode, allowedProviders);
            var created = await policies.CreateAsync(route, policy, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.MfaPolicyCreated, identityScopeId, application,
                application.Value, cancellationToken);
            return created;
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<MfaPolicy>> UpdatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            MfaPolicyMode mode,
            IReadOnlyCollection<AuthenticationFactorProviderKey> allowedProviders,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            EnsureProvidersRegistered(allowedProviders);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policy = new MfaPolicy(identityScopeId, application, mode, allowedProviders);
            var updated = await policies.UpdateAsync(route, policy, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.MfaPolicyUpdated, identityScopeId, application,
                application.Value, cancellationToken);
            return updated;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VersionedRecord<UserAuthenticator>>> ListAuthenticatorsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken)
        {
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            return await authenticators.ListByUserAsync(route, identityScopeId, userId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MfaUserSecurityState> GetUserSecurityStateAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken)
        {
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));

            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policy = await policies.GetAsync(route, identityScopeId, application, cancellationToken);
            var records = await authenticators.ListByUserAsync(route, identityScopeId, userId, cancellationToken);

            if (policy is null || policy.Value.Mode == MfaPolicyMode.Disabled)
            {
                return new MfaUserSecurityState(
                    policyConfigured: policy is not null,
                    policyMode: policy?.Value.Mode,
                    activeVerificationProviders: [],
                    activePrimaryProviders: [],
                    activeRecoveryProviders: []);
            }

            var allowed = policy.Value.AllowedProviders.ToHashSet();
            var verification = new List<AuthenticationFactorProviderKey>();
            var primary = new List<AuthenticationFactorProviderKey>();
            var recovery = new List<AuthenticationFactorProviderKey>();

            foreach (var record in records)
            {
                var authenticator = record.Value;
                if (authenticator.Status != UserAuthenticatorStatus.Active || !allowed.Contains(authenticator.Provider))
                    continue;

                var provider = providers.Find(authenticator.Provider);
                if (provider is null ||
                    (provider.Descriptor.Capabilities & AuthenticationFactorProviderCapabilities.Verification) == 0)
                {
                    continue;
                }

                verification.Add(authenticator.Provider);

                if ((provider.Descriptor.Capabilities & AuthenticationFactorProviderCapabilities.Recovery) != 0)
                    recovery.Add(authenticator.Provider);
                else
                    primary.Add(authenticator.Provider);
            }

            return new MfaUserSecurityState(
                policyConfigured: true,
                policyMode: policy.Value.Mode,
                activeVerificationProviders: verification,
                activePrimaryProviders: primary,
                activeRecoveryProviders: recovery);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<UserAuthenticator>?> RevokeAuthenticatorAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            if (authenticatorId == Guid.Empty) throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            if (expectedVersion < 1) throw new ArgumentOutOfRangeException(nameof(expectedVersion));

            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var policy = await policies.GetAsync(route, identityScopeId, application, cancellationToken);
            var eligibleProviders = ResolveRequiredFactorProviders(policy?.Value);
            var result = await authenticators.RevokeAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                expectedVersion,
                eligibleProviders,
                requireRemainingRequiredFactor: policy?.Value.Mode == MfaPolicyMode.Required,
                timeProvider.GetUtcNow(),
                cancellationToken);

            return await HandleRevocationResultAsync(
                result,
                route,
                identityScopeId,
                application,
                authenticatorId,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<UserAuthenticator>?> RevokeAuthenticatorForRecoveryAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            if (authenticatorId == Guid.Empty) throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            if (expectedVersion < 1) throw new ArgumentOutOfRangeException(nameof(expectedVersion));

            var route = await ResolveAsync(identityScopeId, application, cancellationToken);
            var result = await authenticators.RevokeAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                expectedVersion,
                eligibleRequiredFactorProviders: [],
                requireRemainingRequiredFactor: false,
                timeProvider.GetUtcNow(),
                cancellationToken);

            var updated = await HandleRevocationResultAsync(
                result,
                route,
                identityScopeId,
                application,
                authenticatorId,
                cancellationToken);

            if (updated is not null)
                await sessionAdministration.RevokeUserSessionsAsync(identityScopeId, application, userId, cancellationToken);

            return updated;
        }

        private IReadOnlyCollection<AuthenticationFactorProviderKey> ResolveRequiredFactorProviders(MfaPolicy? policy)
        {
            if (policy is null || policy.Mode == MfaPolicyMode.Disabled)
                return [];

            return policy.AllowedProviders
                .Where(providerKey =>
                {
                    var provider = providers.Find(providerKey);
                    return provider is not null &&
                        (provider.Descriptor.Capabilities & AuthenticationFactorProviderCapabilities.Verification) != 0 &&
                        (provider.Descriptor.Capabilities & AuthenticationFactorProviderCapabilities.Recovery) == 0;
                })
                .ToArray();
        }

        private async Task<VersionedRecord<UserAuthenticator>?> HandleRevocationResultAsync(
            UserAuthenticatorRevocationResult result,
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            Guid authenticatorId,
            CancellationToken cancellationToken)
        {
            switch (result.Decision)
            {
                case UserAuthenticatorRevocationDecision.NotFound:
                    return null;
                case UserAuthenticatorRevocationDecision.VersionConflict:
                    throw new IdentityConcurrencyException();
                case UserAuthenticatorRevocationDecision.WouldViolateRequiredMfa:
                    throw new MfaPolicyComplianceException();
                case UserAuthenticatorRevocationDecision.Succeeded:
                    break;
                default:
                    throw new InvalidOperationException("Unknown authenticator revocation result.");
            }

            if (result.Record is null)
                throw new InvalidOperationException("Successful authenticator revocation did not return a record.");

            await AuditAsync(
                route,
                SecurityAuditEventType.UserAuthenticatorRevoked,
                identityScopeId,
                application,
                authenticatorId.ToString("D"),
                cancellationToken);

            return result.Record;
        }

        private void EnsureProvidersRegistered(IEnumerable<AuthenticationFactorProviderKey> providerKeys)
        {
            foreach (var providerKey in providerKeys)
            {
                if (providers.Find(providerKey) is null)
                    throw new AuthenticationFactorProviderNotRegisteredException();
            }
        }

        private async Task<ResolvedDatabaseRoute> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);

            return await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId, IdentityDataSet.IdentityDirectory),
                cancellationToken);
        }

        private Task<bool> AuditAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEventType eventType,
            Guid identityScopeId,
            ApplicationKey application,
            string target,
            CancellationToken cancellationToken) =>
            auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    eventType,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application: application,
                    targetId: target),
                cancellationToken);
    }
}
