using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed record OrganisationProfileTemplateVersionRow(
        OrganisationProfileTemplateKey TemplateKey,
        OrganisationProfileTemplateVersionNumber Version,
        OrganisationProfileTemplateVersionStatus Status,
        OrganisationProfileContentHash? ContentHash,
        long RowVersion,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? PublishedAt,
        DateTimeOffset? RetiredAt);
}
