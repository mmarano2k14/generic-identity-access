namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines a request to relate an existing tenant membership to an organization.</summary>
    public sealed class CreateOrganizationMembershipRequest
    {
        /// <summary>Gets or sets the existing Identity Access tenant-membership ID.</summary>
        public Guid TenantMembershipId { get; set; }
    }
}
