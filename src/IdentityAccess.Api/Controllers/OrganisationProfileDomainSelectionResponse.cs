using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one exact version-pinned domain selection.</summary>
    public sealed record OrganisationProfileDomainSelectionResponse(
        string DomainKey,
        int DomainVersion)
    {
        /// <summary>Maps one domain selection to the HTTP contract.</summary>
        public static OrganisationProfileDomainSelectionResponse From(
            OrganisationProfileDomainSelection selection) =>
            new(
                selection.DomainKey.Value,
                selection.DomainVersion.Value);
    }
}
