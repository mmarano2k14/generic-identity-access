using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>
    /// Performs atomic group-membership mutations whose validity depends on current persisted
    /// group and tenant-membership state.
    /// </summary>
    public interface IGroupMembershipMutationStore
    {
        /// <summary>
        /// Atomically verifies that the group and tenant membership are active in the same
        /// tenant and inserts the membership edge.
        /// </summary>
        /// <returns>
        /// The persisted edge when all current-state invariants are satisfied; otherwise
        /// <see langword="null"/>.
        /// </returns>
        Task<GroupMembership?> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            GroupReference group,
            Guid tenantMembershipId,
            CancellationToken cancellationToken);
    }
}
