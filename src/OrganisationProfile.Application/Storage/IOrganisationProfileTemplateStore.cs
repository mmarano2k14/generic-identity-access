using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Storage
{
    public interface IOrganisationProfileTemplateStore
    {
        Task<OrganisationProfileTemplate?> GetAsync(
            OrganisationProfileTemplateKey templateKey,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<OrganisationProfileTemplate>> ListAsync(
            int offset,
            int limit,
            CancellationToken cancellationToken);

        Task<OrganisationProfileTemplate> CreateAsync(
            OrganisationProfileTemplate template,
            CancellationToken cancellationToken);

        Task<OrganisationProfileTemplate?> UpdateAsync(
            OrganisationProfileTemplate template,
            CancellationToken cancellationToken);
    }
}
