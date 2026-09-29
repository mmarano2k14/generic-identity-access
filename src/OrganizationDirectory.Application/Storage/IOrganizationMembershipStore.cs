using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Persists explicit organization membership independently from authorization grants.</summary>
    public interface IOrganizationMembershipStore
    {
        /// <summary>Gets one organization membership.</summary>
        Task<OrganizationMembership?> GetAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken);

        /// <summary>Lists organization memberships for one organization.</summary>
        Task<IReadOnlyList<OrganizationMembership>> ListForOrganizationAsync(
            OrganizationReference organization,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Lists organization memberships for one tenant member.</summary>
        Task<IReadOnlyList<OrganizationMembership>> ListForTenantMembershipAsync(
            TenantMembershipReference tenantMembership,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Creates one durable organization membership.</summary>
        Task<OrganizationMembership> CreateAsync(
            OrganizationMembership membership,
            CancellationToken cancellationToken);

        /// <summary>Updates lifecycle state under optimistic concurrency.</summary>
        Task<OrganizationMembership?> UpdateAsync(
            OrganizationMembership membership,
            CancellationToken cancellationToken);

        /// <summary>Deletes one organization membership under optimistic concurrency.</summary>
        Task<bool> DeleteAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            long expectedRowVersion,
            CancellationToken cancellationToken);
    }
}
