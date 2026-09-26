using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persists identity-scope administration policies.</summary>
    public interface IIdentityScopeAdministrationPolicyStore
    {
        /// <summary>Gets a policy.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationPolicy>?> GetAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyReference policy,
            CancellationToken cancellationToken);

        /// <summary>Lists policies for the identity scope and application in a bounded deterministic window.</summary>
        Task<IReadOnlyList<VersionedRecord<IdentityScopeAdministrationPolicy>>> ListAsync(
            ResolvedDatabaseRoute route, Guid identityScopeId, ApplicationKey application, string? search, int offset, int limit,
            CancellationToken cancellationToken);

        /// <summary>Creates a policy.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationPolicy>> CreateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicy policy,
            CancellationToken cancellationToken);

        /// <summary>Updates a policy using optimistic concurrency.</summary>
        Task<VersionedRecord<IdentityScopeAdministrationPolicy>> UpdateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicy policy,
            long expectedVersion,
            CancellationToken cancellationToken);
    }
}
