using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed record OrganisationProfileLatestVersionIdentity(
        OrganisationProfileVersionNumber Version,
        OrganisationProfileContentHash ContentHash);
}
