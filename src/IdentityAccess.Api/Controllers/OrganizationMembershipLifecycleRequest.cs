namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines an organization-membership lifecycle mutation request.</summary>
    public sealed class OrganizationMembershipLifecycleRequest
    {
        /// <summary>Gets or sets the expected optimistic-concurrency row version.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
