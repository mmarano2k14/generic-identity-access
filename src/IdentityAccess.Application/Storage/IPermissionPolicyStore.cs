using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Operation-scoped policy metadata persistence. Authorization remains outside this contract.</summary>
    public interface IPermissionPolicyStore
    {
        /// <summary>Gets the requested permission policy record from the resolved database route.</summary>
        Task<VersionedRecord<PermissionPolicy>?> GetAsync(ResolvedDatabaseRoute route,
            PermissionPolicyReference policy, CancellationToken cancellationToken);

        /// <summary>Lists a bounded window of policies for one tenant and application.</summary>
        Task<IReadOnlyList<VersionedRecord<PermissionPolicy>>> ListAsync(ResolvedDatabaseRoute route,
            TenantReference tenant, ApplicationKey application, int offset, int limit,
            CancellationToken cancellationToken);

        /// <summary>Creates a permission policy record in the resolved database route.</summary>
        Task<VersionedRecord<PermissionPolicy>> CreateAsync(ResolvedDatabaseRoute route,
            PermissionPolicy policy, CancellationToken cancellationToken);

        /// <summary>Updates a permission policy record using optimistic concurrency.</summary>
        Task<VersionedRecord<PermissionPolicy>> UpdateAsync(ResolvedDatabaseRoute route,
            PermissionPolicy policy, long expectedVersion, CancellationToken cancellationToken);
    }
}
