using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Templates
{
    public sealed class OrganisationProfileTemplateDraftService(
        IOrganisationProfileTemplateStore templateStore,
        IOrganisationProfileTemplateVersionStore versionStore,
        IOrganisationProfileClock clock)
    {
        public async Task<OrganisationProfileTemplateVersion> CreateAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            CancellationToken cancellationToken)
        {
            var template = await templateStore.GetAsync(
                    templateKey,
                    cancellationToken)
                .ConfigureAwait(false);

            if (template is null)
                throw new OrganisationProfileTemplateNotFoundException(
                    $"Template '{templateKey}' does not exist.");

            if (template.Status != OrganisationProfileTemplateStatus.Active)
                throw new OrganisationProfileTemplateInactiveException(
                    "New Draft versions require an active template definition.");

            return await versionStore.CreateDraftAsync(
                    OrganisationProfileTemplateVersion.CreateDraft(
                        templateKey,
                        version,
                        domains,
                        clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<OrganisationProfileTemplateVersion?> ReplaceDomainsAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            var current = await versionStore.GetAsync(
                    templateKey,
                    version,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null) return null;

            if (current.Status != OrganisationProfileTemplateVersionStatus.Draft)
                throw new OrganisationProfileTemplateVersionImmutableException(
                    "Published or retired template content is immutable.");

            var updated = new OrganisationProfileTemplateVersion(
                current.TemplateKey,
                current.Version,
                current.Status,
                domains,
                null,
                expectedRowVersion,
                current.CreatedAt,
                clock.UtcNow,
                null,
                null);

            return await versionStore.ReplaceDraftDomainsAsync(
                    updated,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
