namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines template-definition lifecycle mutation.</summary>
    public sealed class OrganisationProfileTemplateLifecycleRequest
    {
        /// <summary>Gets or sets the expected template RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
