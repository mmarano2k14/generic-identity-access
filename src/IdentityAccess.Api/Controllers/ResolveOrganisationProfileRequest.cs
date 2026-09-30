namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines deterministic effective-profile resolution under optimistic concurrency.</summary>
    public sealed class ResolveOrganisationProfileRequest
    {
        /// <summary>Gets or sets the expected mutable profile RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
