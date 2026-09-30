namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines complete replacement of one Draft template version's domain composition.</summary>
    public sealed class ReplaceOrganisationProfileTemplateDomainsRequest
    {
        /// <summary>Gets or sets the expected template-version RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }

        /// <summary>Gets or sets the complete desired domain composition.</summary>
        public IReadOnlyList<OrganisationProfileDomainSelectionRequest> Domains { get; set; } =
            Array.Empty<OrganisationProfileDomainSelectionRequest>();
    }
}
