namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines template-definition metadata update.</summary>
    public sealed class UpdateOrganisationProfileTemplateRequest
    {
        /// <summary>Gets or sets the human-readable template name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Gets or sets the expected template RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
