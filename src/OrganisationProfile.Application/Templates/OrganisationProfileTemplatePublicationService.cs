using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Templates
{
    public sealed class OrganisationProfileTemplatePublicationService(
        IOrganisationProfileTemplateStore templateStore,
        IOrganisationProfileTemplateVersionStore versionStore,
        OrganisationProfileDomainRegistryValidator domainValidator,
        OrganisationProfileTemplateContentHasher contentHasher,
        IOrganisationProfileClock clock)
    {
        public async Task<OrganisationProfileTemplateVersion?> PublishAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            var template = await templateStore.GetAsync(
                    templateKey,
                    cancellationToken)
                .ConfigureAwait(false);

            if (template is null)
                throw new OrganisationProfileTemplateNotFoundException(
                    $"Template '{templateKey}' does not exist.");

            if (template.Status != OrganisationProfileTemplateStatus.Active)
                throw new OrganisationProfileTemplateInactiveException(
                    "Publishing requires an active template definition.");

            var current = await versionStore.GetAsync(
                    templateKey,
                    version,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null) return null;

            if (current.Status != OrganisationProfileTemplateVersionStatus.Draft)
                throw new OrganisationProfileTemplateVersionImmutableException(
                    "Only Draft template versions may be published.");

            if (current.RowVersion != expectedRowVersion)
                throw new OrganisationProfileTemplateVersionConcurrencyException(
                    $"Expected row version {expectedRowVersion}, loaded {current.RowVersion}.");

            await domainValidator.RequireSelectableAsync(
                    current.Domains,
                    cancellationToken)
                .ConfigureAwait(false);

            var now = clock.UtcNow;
            var published = new OrganisationProfileTemplateVersion(
                current.TemplateKey,
                current.Version,
                OrganisationProfileTemplateVersionStatus.Published,
                current.Domains,
                contentHasher.Compute(
                    current.TemplateKey,
                    current.Version,
                    current.Domains),
                expectedRowVersion,
                current.CreatedAt,
                now,
                now,
                null);

            return await versionStore.PublishAsync(
                    published,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<OrganisationProfileTemplateVersion?> RetireAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
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

            if (current.Status != OrganisationProfileTemplateVersionStatus.Published)
                throw new OrganisationProfileTemplateVersionImmutableException(
                    "Only Published template versions may be retired.");

            if (current.RowVersion != expectedRowVersion)
                throw new OrganisationProfileTemplateVersionConcurrencyException(
                    $"Expected row version {expectedRowVersion}, loaded {current.RowVersion}.");

            var now = clock.UtcNow;
            var retired = new OrganisationProfileTemplateVersion(
                current.TemplateKey,
                current.Version,
                OrganisationProfileTemplateVersionStatus.Retired,
                current.Domains,
                current.ContentHash,
                expectedRowVersion,
                current.CreatedAt,
                now,
                current.PublishedAt,
                now);

            return await versionStore.RetireAsync(
                    retired,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
