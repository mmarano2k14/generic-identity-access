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
            var existing = await authenticators.GetAsync(route, identityScopeId, authenticatorId, cancellationToken);
            if (existing is null || existing.Value.UserId != userId) return null;
            if (existing.Version != expectedVersion) throw new IdentityConcurrencyException();
            if (existing.Value.Status == UserAuthenticatorStatus.Revoked) return existing;

            var value = existing.Value;
            var revoked = new UserAuthenticator(
                value.IdentityScopeId,
                value.AuthenticatorId,
                value.UserId,
                value.Provider,
                value.DisplayName,
                UserAuthenticatorStatus.Revoked,
                value.CreatedAt,
                value.ConfirmedAt,
                value.LastUsedAt,
                timeProvider.GetUtcNow());

            var updated = await authenticators.UpdateAsync(route, revoked, expectedVersion, cancellationToken);
            await AuditAsync(route, SecurityAuditEventType.UserAuthenticatorRevoked, identityScopeId, application,
                authenticatorId.ToString("D"), cancellationToken);
            return updated;
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
