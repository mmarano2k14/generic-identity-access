using OrganisationProfile.Domain;

namespace OrganisationProfile.Application
{
    /// <summary>Minimal external Organization state required by OrganisationProfile lifecycle validation.</summary>
    public sealed record OrganizationReferenceState(
        OrganizationReference Reference,
        bool IsActive);
}
