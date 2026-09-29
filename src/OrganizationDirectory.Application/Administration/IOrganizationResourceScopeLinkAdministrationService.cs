using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Administration
{
    /// <summary>Defines application-aware Organization-to-ResourceScope linkage administration.</summary>
    public interface IOrganizationResourceScopeLinkAdministrationService
    {
        /// <summary>Gets the current application's ResourceScope link for one Organization.</summary>
        Task<OrganizationResourceScopeLink?> GetAsync(
            OrganizationReference organization,
            ApplicationKey application,
            CancellationToken cancellationToken);

        /// <summary>Lists all application-aware ResourceScope links for one Organization.</summary>
        Task<IReadOnlyList<OrganizationResourceScopeLink>> ListAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken);

        /// <summary>Links an active Organization to an active Identity Access ResourceScope.</summary>
        Task<OrganizationResourceScopeLink> LinkAsync(
            OrganizationReference organization,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            CancellationToken cancellationToken);

        /// <summary>Replaces the current application's ResourceScope link under optimistic concurrency.</summary>
        Task<OrganizationResourceScopeLink?> RelinkAsync(
            OrganizationReference organization,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            long expectedRowVersion,
            CancellationToken cancellationToken);

        /// <summary>Removes the current application's ResourceScope link.</summary>
        Task<bool> RemoveAsync(
            OrganizationReference organization,
            ApplicationKey application,
            long expectedRowVersion,
            CancellationToken cancellationToken);
    }
}
