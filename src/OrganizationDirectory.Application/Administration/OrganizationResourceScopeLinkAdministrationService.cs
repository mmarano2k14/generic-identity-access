using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Administration
{
    /// <summary>Coordinates Organization-to-ResourceScope linkage without owning authorization decisions.</summary>
    public sealed class OrganizationResourceScopeLinkAdministrationService(
        IOrganizationStore organizationStore,
        IOrganizationResourceScopeLinkStore linkStore,
        IResourceScopeReferenceReader resourceScopeReader,
        IOrganizationClock clock) : IOrganizationResourceScopeLinkAdministrationService
    {
        /// <inheritdoc />
        public async Task<OrganizationResourceScopeLink?> GetAsync(
            OrganizationReference organization,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(application);

            await RequireOrganizationAsync(organization, requireActive: false, cancellationToken)
                .ConfigureAwait(false);

            return await linkStore.GetAsync(
                    organization,
                    application,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganizationResourceScopeLink>> ListAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);

            await RequireOrganizationAsync(organization, requireActive: false, cancellationToken)
                .ConfigureAwait(false);

            return await linkStore.ListAsync(organization, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OrganizationResourceScopeLink> LinkAsync(
            OrganizationReference organization,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(application);

            await RequireOrganizationAsync(organization, requireActive: true, cancellationToken)
                .ConfigureAwait(false);

            var scope = await RequireActiveResourceScopeAsync(
                    organization,
                    application,
                    resourceScopeId,
                    cancellationToken)
                .ConfigureAwait(false);

            var now = clock.UtcNow;

            var link = new OrganizationResourceScopeLink(
                organization,
                scope.Reference,
                OrganizationResourceScopeLinkStatus.Active,
                rowVersion: 0,
                createdAt: now,
                updatedAt: now);

            return await linkStore.CreateAsync(link, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OrganizationResourceScopeLink?> RelinkAsync(
            OrganizationReference organization,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(application);
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            await RequireOrganizationAsync(organization, requireActive: true, cancellationToken)
                .ConfigureAwait(false);

            var current = await linkStore.GetAsync(
                    organization,
                    application,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return null;
            }

            var scope = await RequireActiveResourceScopeAsync(
                    organization,
                    application,
                    resourceScopeId,
                    cancellationToken)
                .ConfigureAwait(false);

            var updated = new OrganizationResourceScopeLink(
                organization,
                scope.Reference,
                current.Status,
                expectedRowVersion,
                current.CreatedAt,
                clock.UtcNow);

            return await linkStore.UpdateAsync(updated, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<bool> RemoveAsync(
            OrganizationReference organization,
            ApplicationKey application,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(application);
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            return linkStore.DeleteAsync(
                organization,
                application,
                expectedRowVersion,
                cancellationToken);
        }

        private async Task<Organization> RequireOrganizationAsync(
            OrganizationReference reference,
            bool requireActive,
            CancellationToken cancellationToken)
        {
            var organization = await organizationStore.GetAsync(reference, cancellationToken)
                .ConfigureAwait(false);

            if (organization is null)
            {
                throw new OrganizationResourceScopeOrganizationNotFoundException(
                    $"Organization '{reference.OrganizationId}' was not found in the tenant.");
            }

            if (requireActive && organization.Status != OrganizationStatus.Active)
            {
                throw new OrganizationResourceScopeOrganizationInactiveException(
                    "ResourceScope linkage can only be changed for an active Organization.");
            }

            return organization;
        }

        private async Task<ResourceScopeReferenceState> RequireActiveResourceScopeAsync(
            OrganizationReference organization,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            CancellationToken cancellationToken)
        {
            var scope = await resourceScopeReader.GetAsync(
                    organization.IdentityScopeId,
                    organization.TenantId,
                    application,
                    resourceScopeId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (scope is null)
            {
                throw new OrganizationResourceScopeReferenceNotFoundException(
                    $"ResourceScope '{resourceScopeId}' was not found for application '{application}' in this tenant.");
            }

            if (!scope.IsActive)
            {
                throw new OrganizationResourceScopeInactiveException(
                    $"ResourceScope '{resourceScopeId}' is inactive.");
            }

            return scope;
        }
    }
}
