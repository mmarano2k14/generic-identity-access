using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>
    /// Resolves whether tenant visibility is identity-scope-wide or membership-limited without
    /// replacing final RBAC authorization for any operation.
    /// </summary>
    public sealed class EffectiveAdministrationContextService(
        IDatabaseRouteResolver routeResolver,
        IIdentityScopeAssignedCapabilityReader scopeGrants,
        ITenantMembershipStore tenantMemberships)
        : IEffectiveAdministrationContextService
    {
        /// <inheritdoc />
        public async Task<EffectiveAdministrationContext> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            SubjectReference subject,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(subject);

            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("An identity scope is required.", nameof(identityScopeId));

            if (subject.IdentityScopeId != identityScopeId)
                throw new ArgumentException("The subject must belong to the requested identity scope.", nameof(subject));

            var route = await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId), cancellationToken).ConfigureAwait(false);

            var grants = await scopeGrants.ListAsync(
                route, identityScopeId, subject, application, cancellationToken).ConfigureAwait(false);

            var memberships = await tenantMemberships.ListForSubjectAsync(
                route, subject, cancellationToken).ConfigureAwait(false);

            var activeMemberships = memberships
                .Where(record => record.Value.Status == MembershipStatus.Active)
                .Select(record => new AdministrationTenantMembershipReference(
                    record.Value.MembershipId,
                    record.Value.Tenant))
                .ToArray();

            var visibility = grants.Count == 0
                ? AdministrationTenantVisibilityMode.MembershipLimited
                : AdministrationTenantVisibilityMode.ScopeWide;

            return new EffectiveAdministrationContext(subject, application, visibility, activeMemberships);
        }
    }
}
