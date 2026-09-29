using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Administration
{
    /// <summary>Defines explicit organization-membership administration independently from authorization grants.</summary>
    public interface IOrganizationMembershipAdministrationService
    {
        /// <summary>Lists members related to one organization.</summary>
        Task<IReadOnlyList<OrganizationMembership>> ListForOrganizationAsync(
            OrganizationReference organization,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Lists organizations related to one tenant membership.</summary>
        Task<IReadOnlyList<OrganizationMembership>> ListForTenantMembershipAsync(
            TenantMembershipReference tenantMembership,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Gets one explicit organization-membership relation.</summary>
        Task<OrganizationMembership?> GetAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken);

        /// <summary>Adds one active tenant member to an active organization.</summary>
        Task<OrganizationMembership> AddAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken);

        /// <summary>Changes organization-membership lifecycle state.</summary>
        Task<OrganizationMembership?> SetStatusAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            OrganizationMembershipStatus status,
            long expectedRowVersion,
            CancellationToken cancellationToken);

        /// <summary>Removes one organization-membership relation without deleting the user or tenant membership.</summary>
        Task<bool> RemoveAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            long expectedRowVersion,
            CancellationToken cancellationToken);
    }
}
