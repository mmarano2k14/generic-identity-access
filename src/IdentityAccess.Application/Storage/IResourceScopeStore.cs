using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Defines the contract for resource scope store.</summary>
    public interface IResourceScopeStore
    {
        /// <summary>Gets the requested resource scope record from the resolved database route.</summary>
        Task<VersionedRecord<ResourceScope>?> GetAsync(ResolvedDatabaseRoute route, ResourceScopeReference reference,
            CancellationToken cancellationToken);

        /// <summary>Lists resource scope records for the supplied scope.</summary>
        Task<IReadOnlyList<VersionedRecord<ResourceScope>>> ListAsync(ResolvedDatabaseRoute route,
            TenantReference tenant, ApplicationKey application, string? search, int offset, int limit,
            CancellationToken cancellationToken);

        /// <summary>Creates a resource scope record in the resolved database route.</summary>
        Task<VersionedRecord<ResourceScope>> CreateAsync(ResolvedDatabaseRoute route, ResourceScope scope,
            CancellationToken cancellationToken);

        /// <summary>Updates a resource scope record using optimistic concurrency.</summary>
        Task<VersionedRecord<ResourceScope>> UpdateAsync(ResolvedDatabaseRoute route, ResourceScope scope,
            long expectedVersion, CancellationToken cancellationToken);
    }
}
