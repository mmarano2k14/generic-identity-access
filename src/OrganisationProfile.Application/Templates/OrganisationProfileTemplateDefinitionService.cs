using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Templates
{
    public sealed class OrganisationProfileTemplateDefinitionService(
        IOrganisationProfileTemplateStore store,
        IOrganisationProfileClock clock)
    {
        public Task<OrganisationProfileTemplate> CreateAsync(
            OrganisationProfileTemplateKey templateKey,
            string displayName,
            CancellationToken cancellationToken) =>
            store.CreateAsync(
                OrganisationProfileTemplate.Create(
                    templateKey,
                    displayName,
                    clock.UtcNow),
                cancellationToken);

        public async Task<OrganisationProfileTemplate?> RenameAsync(
            OrganisationProfileTemplateKey templateKey,
            string displayName,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            var current = await store.GetAsync(templateKey, cancellationToken)
                .ConfigureAwait(false);

            if (current is null) return null;

            return await store.UpdateAsync(
                    new OrganisationProfileTemplate(
                        current.TemplateKey,
                        displayName,
                        current.Status,
                        expectedRowVersion,
                        current.CreatedAt,
                        clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<OrganisationProfileTemplate?> SetStatusAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateStatus status,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status));

            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            var current = await store.GetAsync(templateKey, cancellationToken)
                .ConfigureAwait(false);

            if (current is null) return null;

            return await store.UpdateAsync(
                    new OrganisationProfileTemplate(
                        current.TemplateKey,
                        current.DisplayName,
                        status,
                        expectedRowVersion,
                        current.CreatedAt,
                        clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
