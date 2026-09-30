namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines replacement or removal of one OrganisationProfile template pin.</summary>
    public sealed class SetOrganisationProfileTemplateRequest
    {
        /// <summary>Gets or sets the optional template key. Null removes the pin.</summary>
        public string? TemplateKey { get; set; }

        /// <summary>Gets or sets the optional exact template version. Null removes the pin.</summary>
        public int? TemplateVersion { get; set; }

        /// <summary>Gets or sets the expected profile RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
