namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines creation of one reusable OrganisationProfile template definition.</summary>
    public sealed class CreateOrganisationProfileTemplateRequest
    {
        /// <summary>Gets or sets the stable template key.</summary>
        public string TemplateKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the human-readable template name.</summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}
