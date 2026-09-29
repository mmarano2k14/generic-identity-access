namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines a ResourceScope relink request under optimistic concurrency.</summary>
    public sealed class UpdateOrganizationResourceScopeLinkRequest
    {
        /// <summary>Gets or sets the replacement Identity Access ResourceScope ID.</summary>
        public Guid ResourceScopeId { get; set; }

        /// <summary>Gets or sets the expected link row version.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
