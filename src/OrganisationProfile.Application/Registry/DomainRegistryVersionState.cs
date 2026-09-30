using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Registry
{
    /// <summary>Minimal external Domain Registry state required by OrganisationProfile.</summary>
    public sealed record DomainRegistryVersionState(
        DomainKey DomainKey,
        DomainVersion DomainVersion,
        DomainRegistryVersionStatus Status);
}
