using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Durable organization persistence boundary.</summary>
    public interface IOrganizationStore
    {
        /// <summary>Gets one organization by stable reference.</summary>
        Task<Organization?> GetAsync(OrganizationReference reference, CancellationToken cancellationToken);

        /// <summary>Gets one organization by tenant-local key.</summary>
        Task<Organization?> FindByKeyAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            OrganizationKey key,
            CancellationToken cancellationToken);

        /// <summary>Lists organizations for one tenant using bounded paging.</summary>
        Task<IReadOnlyList<Organization>> ListAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Creates a new organization and returns the persisted state.</summary>
        Task<Organization> CreateAsync(Organization organization, CancellationToken cancellationToken);

        /// <summary>Updates an existing organization using its row version as the concurrency token.</summary>
        Task<Organization?> UpdateAsync(Organization organization, CancellationToken cancellationToken);

        /// <summary>Deletes an organization when the expected row version still matches.</summary>
        Task<bool> DeleteAsync(
            OrganizationReference reference,
            long expectedRowVersion,
            CancellationToken cancellationToken);
    }
}
