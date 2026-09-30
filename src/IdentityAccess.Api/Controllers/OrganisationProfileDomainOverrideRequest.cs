using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Defines one domain override in a full replacement request.</summary>
    public sealed class OrganisationProfileDomainOverrideRequest
    {
        /// <summary>Gets or sets the target domain key.</summary>
        public string DomainKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the required version for Enable, or null for Disable.</summary>
        public int? DomainVersion { get; set; }

        /// <summary>Gets or sets Enable or Disable semantics.</summary>
        public OrganisationProfileDomainOverrideOperation Operation { get; set; }
    }
}
