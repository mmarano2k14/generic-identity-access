namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines an organization definition update.</summary>
    public sealed class UpdateOrganizationRequest
    {
        /// <summary>Gets or sets the display name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Gets or sets the application-defined organization type.</summary>
        public string OrganizationType { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional parent organization ID.</summary>
        public Guid? ParentOrganizationId { get; set; }

        /// <summary>Gets or sets the expected optimistic-concurrency version.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
