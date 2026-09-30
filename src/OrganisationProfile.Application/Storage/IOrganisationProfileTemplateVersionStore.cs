using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Storage
{
    public interface IOrganisationProfileTemplateVersionStore
    {
        Task<OrganisationProfileTemplateVersion?> GetAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<OrganisationProfileTemplateVersion>> ListAsync(
            OrganisationProfileTemplateKey templateKey,
            CancellationToken cancellationToken);

        Task<OrganisationProfileTemplateVersion> CreateDraftAsync(
            OrganisationProfileTemplateVersion draft,
            CancellationToken cancellationToken);

        Task<OrganisationProfileTemplateVersion?> ReplaceDraftDomainsAsync(
            OrganisationProfileTemplateVersion draft,
            CancellationToken cancellationToken);

        Task<OrganisationProfileTemplateVersion?> PublishAsync(
            OrganisationProfileTemplateVersion published,
            CancellationToken cancellationToken);

        Task<OrganisationProfileTemplateVersion?> RetireAsync(
            OrganisationProfileTemplateVersion retired,
            CancellationToken cancellationToken);
    }
}
