namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines publish or retire mutation for one template version.</summary>
    public sealed class OrganisationProfileTemplateVersionLifecycleRequest
    {
        /// <summary>Gets or sets the expected template-version RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
