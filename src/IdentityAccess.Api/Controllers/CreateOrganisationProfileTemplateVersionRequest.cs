namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines creation of one Draft template version.</summary>
    public sealed class CreateOrganisationProfileTemplateVersionRequest
    {
        /// <summary>Gets or sets the explicit positive template version.</summary>
        public int TemplateVersion { get; set; }

        /// <summary>Gets or sets the complete initial Draft domain composition.</summary>
        public IReadOnlyList<OrganisationProfileDomainSelectionRequest> Domains { get; set; } =
            Array.Empty<OrganisationProfileDomainSelectionRequest>();
    }
}
