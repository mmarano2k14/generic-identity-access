namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines an organization creation request.</summary>
    public sealed class CreateOrganizationRequest
    {
        /// <summary>Gets or sets the optional caller-supplied organization ID.</summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>Gets or sets the tenant-local stable organization key.</summary>
        public string OrganizationKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the display name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Gets or sets the application-defined organization type.</summary>
        public string OrganizationType { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional parent organization ID.</summary>
        public Guid? ParentOrganizationId { get; set; }
    }
}
