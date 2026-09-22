using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persists identity-scope administration groups.</summary>
    public interface IIdentityScopeAdministrationGroupStore
    {
        /// <summary>Gets a group.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationGroup>?> GetAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group,
            CancellationToken cancellationToken);

        /// <summary>Creates a group.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationGroup>> CreateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroup group,
            CancellationToken cancellationToken);

        /// <summary>Updates a group using optimistic concurrency.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationGroup>> UpdateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroup group,
            long expectedVersion,
            CancellationToken cancellationToken);
    }
}
