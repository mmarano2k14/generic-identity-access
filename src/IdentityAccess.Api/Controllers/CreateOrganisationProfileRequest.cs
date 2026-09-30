namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines creation of one tenant-owned OrganisationProfile.</summary>
    public sealed class CreateOrganisationProfileRequest
    {
        /// <summary>Gets or sets the optional caller-supplied profile ID.</summary>
        public Guid? OrganisationProfileId { get; set; }

        /// <summary>Gets or sets the Organization identity that owns semantic profile configuration.</summary>
        public Guid OrganizationId { get; set; }

        /// <summary>Gets or sets the optional template key.</summary>
        public string? TemplateKey { get; set; }

        /// <summary>Gets or sets the optional exact template version.</summary>
        public int? TemplateVersion { get; set; }
    }
}
