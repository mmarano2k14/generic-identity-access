using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Operation-scoped user persistence. Authorization remains outside this contract.</summary>
    public interface IUserDirectoryStore
    {
        /// <summary>Gets the requested user record from the resolved database route.</summary>
        Task<VersionedRecord<User>?> GetAsync(ResolvedDatabaseRoute route, SubjectReference subject,
            CancellationToken cancellationToken);

        /// <summary>Lists a bounded window of users in the resolved identity scope.</summary>
        Task<IReadOnlyList<VersionedRecord<User>>> ListAsync(ResolvedDatabaseRoute route, Guid identityScopeId,
            int offset, int limit, CancellationToken cancellationToken);

        /// <summary>Creates a user record in the resolved database route.</summary>
        Task<VersionedRecord<User>> CreateAsync(ResolvedDatabaseRoute route, User user,
            CancellationToken cancellationToken);

        /// <summary>Updates a user record using optimistic concurrency.</summary>
        Task<VersionedRecord<User>> UpdateAsync(ResolvedDatabaseRoute route, User user, long expectedVersion,
            CancellationToken cancellationToken);
    }
}
