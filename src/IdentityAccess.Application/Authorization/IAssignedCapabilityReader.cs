using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authorization
{

    /// <summary>
    /// Projects currently assigned capability grants for one subject and optional resource target.
    /// The reader filters tenant-wide, exact-scope and explicitly inherited bindings but does not evaluate RBAC wildcards.
    /// </summary>
    public interface IAssignedCapabilityReader
    {
        /// <summary>Lists effective capability grants for the subject after tenant, group, policy, and resource-scope filtering.</summary>
        Task<IReadOnlyList<AssignedCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            SubjectReference subject,
            ApplicationKey application,
            ResourceScopeReference? resourceScope,
            CancellationToken cancellationToken);
    }
}
