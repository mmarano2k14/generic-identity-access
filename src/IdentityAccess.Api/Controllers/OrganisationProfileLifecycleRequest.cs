namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines an OrganisationProfile lifecycle mutation.</summary>
    public sealed class OrganisationProfileLifecycleRequest
    {
        /// <summary>Gets or sets the expected profile RowVersion.</summary>
        public long ExpectedRowVersion { get; set; }
    }
}
