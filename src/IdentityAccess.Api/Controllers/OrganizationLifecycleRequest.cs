namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines an organization lifecycle mutation request.</summary>
    public sealed class OrganizationLifecycleRequest
    {
        /// <summary>Gets or sets the expected optimistic-concurrency version.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
