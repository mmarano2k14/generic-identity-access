using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Resolves tenant association from trusted directory state without making RBAC decisions.</summary>
    public sealed class AdministrationTenantVisibilityService(
        IDatabaseRouteResolver routeResolver,
        ITenantMembershipStore tenantMemberships)
        : IAdministrationTenantVisibilityService
    {
        /// <inheritdoc />
        public async Task<bool> HasActiveMembershipAsync(
            Guid identityScopeId,
            ApplicationKey application,
            SubjectReference subject,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(subject);

            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("An identity scope is required.", nameof(identityScopeId));
            if (tenantId == Guid.Empty)
                throw new ArgumentException("A tenant is required.", nameof(tenantId));
            if (subject.IdentityScopeId != identityScopeId)
                throw new ArgumentException("The subject must belong to the requested identity scope.", nameof(subject));

            var route = await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId), cancellationToken).ConfigureAwait(false);

            var membership = await tenantMemberships.FindAsync(
                route,
                new TenantReference(identityScopeId, tenantId),
                subject,
                cancellationToken).ConfigureAwait(false);

            return membership?.Value.Status == MembershipStatus.Active;
        }
    }
}
