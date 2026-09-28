using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>
    /// Persistence for immutable group-membership edges. Add/remove are exact operations and carry no mutable row state.
    /// Authorization remains outside this contract.
    /// </summary>
    public interface IGroupMembershipStore
    {
        /// <summary>Adds a group membership record to the resolved database route.</summary>
        Task AddAsync(ResolvedDatabaseRoute route, GroupMembership membership,
            CancellationToken cancellationToken);

        /// <summary>Removes a group membership record from the resolved database route.</summary>
        Task<bool> RemoveAsync(ResolvedDatabaseRoute route, GroupReference group, Guid tenantMembershipId,
            CancellationToken cancellationToken);

        /// <summary>Lists group membership records for the supplied scope.</summary>
        Task<IReadOnlyList<GroupMembership>> ListAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken);

        /// <summary>Lists all group-membership edges for one tenant/application boundary.</summary>
        Task<IReadOnlyList<GroupMembership>> ListForTenantAsync(ResolvedDatabaseRoute route, TenantReference tenant,
            ApplicationKey application, CancellationToken cancellationToken);
    }
}
