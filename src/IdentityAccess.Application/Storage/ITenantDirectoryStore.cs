using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Operation-scoped tenant persistence. Authorization remains outside this contract.</summary>
    public interface ITenantDirectoryStore
    {
        /// <summary>Gets the requested tenant record from the resolved database route.</summary>
        Task<VersionedRecord<Tenant>?> GetAsync(ResolvedDatabaseRoute route, TenantReference tenant,
            CancellationToken cancellationToken);

        /// <summary>Creates a tenant record in the resolved database route.</summary>
        Task<VersionedRecord<Tenant>> CreateAsync(ResolvedDatabaseRoute route, Tenant tenant,
            CancellationToken cancellationToken);

        /// <summary>Updates a tenant record using optimistic concurrency.</summary>
        Task<VersionedRecord<Tenant>> UpdateAsync(ResolvedDatabaseRoute route, Tenant tenant, long expectedVersion,
            CancellationToken cancellationToken);
    }
}
