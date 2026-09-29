namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines a request to link one Organization to an existing ResourceScope.</summary>
    public sealed class CreateOrganizationResourceScopeLinkRequest
    {
        /// <summary>Gets or sets the existing Identity Access ResourceScope ID.</summary>
        public Guid ResourceScopeId { get; set; }
    }
}
