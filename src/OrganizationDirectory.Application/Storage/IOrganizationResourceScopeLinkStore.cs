using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Persists application-aware Organization-to-ResourceScope links.</summary>
    public interface IOrganizationResourceScopeLinkStore
    {
        /// <summary>Gets the link for one Organization and application.</summary>
        Task<OrganizationResourceScopeLink?> GetAsync(
            OrganizationReference organization,
            ApplicationKey application,
            CancellationToken cancellationToken);

        /// <summary>Lists all application-aware ResourceScope links for one Organization.</summary>
        Task<IReadOnlyList<OrganizationResourceScopeLink>> ListAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken);

        /// <summary>Creates one Organization-to-ResourceScope link.</summary>
        Task<OrganizationResourceScopeLink> CreateAsync(
            OrganizationResourceScopeLink link,
            CancellationToken cancellationToken);

        /// <summary>Updates one Organization-to-ResourceScope link under optimistic concurrency.</summary>
        Task<OrganizationResourceScopeLink?> UpdateAsync(
            OrganizationResourceScopeLink link,
            CancellationToken cancellationToken);

        /// <summary>Deletes one Organization-to-ResourceScope link under optimistic concurrency.</summary>
        Task<bool> DeleteAsync(
            OrganizationReference organization,
            ApplicationKey application,
            long expectedRowVersion,
            CancellationToken cancellationToken);
    }
}
