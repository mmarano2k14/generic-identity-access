using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Administration
{
    /// <summary>Defines application operations for organization identity and hierarchy administration.</summary>
    public interface IOrganizationAdministrationService
    {
        /// <summary>Lists organizations in one tenant using bounded paging.</summary>
        Task<IReadOnlyList<Organization>> ListAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Gets one organization by stable identity.</summary>
        Task<Organization?> GetAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken);

        /// <summary>Lists direct child organizations.</summary>
        Task<IReadOnlyList<Organization>> ListChildrenAsync(
            OrganizationReference parent,
            CancellationToken cancellationToken);

        /// <summary>Builds the current tenant organization forest.</summary>
        Task<IReadOnlyList<OrganizationTreeNode>> GetTreeAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            CancellationToken cancellationToken);

        /// <summary>Creates an organization.</summary>
        Task<Organization> CreateAsync(
            OrganizationReference reference,
            OrganizationKey key,
            string displayName,
            OrganizationType type,
            OrganizationId? parentOrganizationId,
            CancellationToken cancellationToken);

        /// <summary>Updates mutable organization definition state while preserving its stable key.</summary>
        Task<Organization?> UpdateAsync(
            OrganizationReference reference,
            string displayName,
            OrganizationType type,
            OrganizationId? parentOrganizationId,
            long expectedRowVersion,
            CancellationToken cancellationToken);

        /// <summary>Changes organization lifecycle state.</summary>
        Task<Organization?> SetStatusAsync(
            OrganizationReference reference,
            OrganizationStatus status,
            long expectedRowVersion,
            CancellationToken cancellationToken);
    }
}
