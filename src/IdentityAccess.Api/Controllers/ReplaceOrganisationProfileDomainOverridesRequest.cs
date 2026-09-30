namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines full replacement of one profile's Organization-specific domain overrides.</summary>
    public sealed class ReplaceOrganisationProfileDomainOverridesRequest
    {
        /// <summary>Gets or sets the expected mutable profile RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }

        /// <summary>Gets or sets the complete desired override set.</summary>
        public IReadOnlyList<OrganisationProfileDomainOverrideRequest> Overrides { get; set; } =
            Array.Empty<OrganisationProfileDomainOverrideRequest>();
    }
}
