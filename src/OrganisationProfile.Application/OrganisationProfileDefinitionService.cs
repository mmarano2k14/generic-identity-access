using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application
{
    /// <summary>Owns mutable OrganisationProfile definition lifecycle without owning HTTP or RBAC.</summary>
    public sealed class OrganisationProfileDefinitionService(
        IOrganisationProfileStore profileStore,
        IOrganizationReferenceReader organizationReader,
        IOrganisationProfileTemplateStore templateStore,
        IOrganisationProfileTemplateVersionStore templateVersionStore,
        IOrganisationProfileClock clock)
    {
        /// <summary>Creates one active profile attached to an active Organization.</summary>
        public async Task<Domain.OrganisationProfile> CreateAsync(
            OrganisationProfileId organisationProfileId,
            OrganizationReference organization,
            OrganisationProfileTemplatePin? templatePin,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);

            await RequireActiveOrganizationAsync(
                    organization,
                    cancellationToken)
                .ConfigureAwait(false);

            await RequireSelectableTemplatePinAsync(
                    templatePin,
                    cancellationToken)
                .ConfigureAwait(false);

            return await profileStore.CreateAsync(
                    Domain.OrganisationProfile.Create(
                        organisationProfileId,
                        organization,
                        templatePin,
                        clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>Changes the profile template pin under optimistic concurrency.</summary>
        public async Task<Domain.OrganisationProfile?> SetTemplateAsync(
            OrganisationProfileId organisationProfileId,
            OrganisationProfileTemplatePin? templatePin,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            if (expectedRowVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedRowVersion));
            }

            var current = await profileStore.GetAsync(
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return null;
            }

            if (current.Status != OrganisationProfileStatus.Active)
            {
                throw new OrganisationProfileInactiveException(
                    "Template pin may only be changed for an active OrganisationProfile.");
            }

            if (current.RowVersion != expectedRowVersion)
            {
                throw new OrganisationProfileConcurrencyException(
                    $"OrganisationProfile expected row version {expectedRowVersion} " +
                    $"but durable row version is {current.RowVersion}.");
            }

            await RequireSelectableTemplatePinAsync(
                    templatePin,
                    cancellationToken)
                .ConfigureAwait(false);

            return await profileStore.UpdateAsync(
                    new Domain.OrganisationProfile(
                        current.OrganisationProfileId,
                        current.Organization,
                        templatePin,
                        current.Status,
                        expectedRowVersion,
                        current.CreatedAt,
                        clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>Changes profile lifecycle status while preserving durable identity.</summary>
        public async Task<Domain.OrganisationProfile?> SetStatusAsync(
            OrganisationProfileId organisationProfileId,
            OrganisationProfileStatus status,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            if (expectedRowVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedRowVersion));
            }

            var current = await profileStore.GetAsync(
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return null;
            }

            if (current.RowVersion != expectedRowVersion)
            {
                throw new OrganisationProfileConcurrencyException(
                    $"OrganisationProfile expected row version {expectedRowVersion} " +
                    $"but durable row version is {current.RowVersion}.");
            }

            if (status == OrganisationProfileStatus.Active)
            {
                await RequireActiveOrganizationAsync(
                        current.Organization,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return await profileStore.UpdateAsync(
                    new Domain.OrganisationProfile(
                        current.OrganisationProfileId,
                        current.Organization,
                        current.TemplatePin,
                        status,
                        expectedRowVersion,
                        current.CreatedAt,
                        clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task RequireActiveOrganizationAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken)
        {
            var state = await organizationReader.GetAsync(
                    organization,
                    cancellationToken)
                .ConfigureAwait(false);

            if (state is null)
            {
                throw new OrganisationProfileOrganizationNotFoundException(
                    "The referenced Organization does not exist.");
            }

            if (!state.IsActive)
            {
                throw new OrganisationProfileOrganizationInactiveException(
                    "The referenced Organization is disabled.");
            }
        }

        private async Task RequireSelectableTemplatePinAsync(
            OrganisationProfileTemplatePin? templatePin,
            CancellationToken cancellationToken)
        {
            if (templatePin is null)
            {
                return;
            }

            var template = await templateStore.GetAsync(
                    templatePin.TemplateKey,
                    cancellationToken)
                .ConfigureAwait(false);

            if (template is null)
            {
                throw new OrganisationProfileTemplateNotFoundException(
                    $"Template '{templatePin.TemplateKey}' does not exist.");
            }

            if (template.Status != OrganisationProfileTemplateStatus.Active)
            {
                throw new OrganisationProfileTemplateInactiveException(
                    $"Template '{templatePin.TemplateKey}' is disabled.");
            }

            var version = await templateVersionStore.GetAsync(
                    templatePin.TemplateKey,
                    templatePin.Version,
                    cancellationToken)
                .ConfigureAwait(false);

            if (version is null)
            {
                throw new OrganisationProfileTemplateVersionNotFoundException(
                    $"Template version '{templatePin.TemplateKey}@{templatePin.Version}' does not exist.");
            }

            if (version.Status != OrganisationProfileTemplateVersionStatus.Published)
            {
                throw new OrganisationProfileTemplateVersionNotPublishedException(
                    "New profile pins require a Published template version.");
            }
        }
    }
}
