using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Operation-scoped user-group persistence. Authorization remains outside this contract.</summary>
    public interface IUserGroupStore
    {
        /// <summary>Gets the requested user group record from the resolved database route.</summary>
        Task<VersionedRecord<UserGroup>?> GetAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken);

        /// <summary>Creates a user group record in the resolved database route.</summary>
        Task<VersionedRecord<UserGroup>> CreateAsync(ResolvedDatabaseRoute route, UserGroup group,
            CancellationToken cancellationToken);

        /// <summary>Updates a user group record using optimistic concurrency.</summary>
        Task<VersionedRecord<UserGroup>> UpdateAsync(ResolvedDatabaseRoute route, UserGroup group, long expectedVersion,
            CancellationToken cancellationToken);
    }
}
