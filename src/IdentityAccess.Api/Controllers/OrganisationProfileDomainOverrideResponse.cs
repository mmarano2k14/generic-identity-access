using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one Organization-specific domain override.</summary>
    public sealed record OrganisationProfileDomainOverrideResponse(
        string DomainKey,
        int? DomainVersion,
        OrganisationProfileDomainOverrideOperation Operation)
    {
        /// <summary>Maps one domain override to the HTTP contract.</summary>
        public static OrganisationProfileDomainOverrideResponse From(
            OrganisationProfileDomainOverride item) =>
            new(
                item.DomainKey.Value,
                item.DomainVersion?.Value,
                item.Operation);
    }
}
