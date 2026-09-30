using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one immutable OrganisationProfile template pin.</summary>
    public sealed record OrganisationProfileTemplatePinResponse(
        string TemplateKey,
        int TemplateVersion)
    {
        /// <summary>Maps a domain pin to the HTTP contract.</summary>
        public static OrganisationProfileTemplatePinResponse From(
            OrganisationProfileTemplatePin pin) =>
            new(
                pin.TemplateKey.Value,
                pin.Version.Value);
    }
}
