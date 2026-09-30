using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Registry
{
    /// <summary>Raised when an exact domain version cannot satisfy profile-composition policy.</summary>
    public sealed class DomainRegistryVersionUnavailableException : Exception
    {
        /// <summary>Gets the unavailable domain key.</summary>
        public DomainKey DomainKey { get; }

        /// <summary>Gets the unavailable domain version.</summary>
        public DomainVersion DomainVersion { get; }

        /// <summary>Initializes one unavailable-domain error.</summary>
        public DomainRegistryVersionUnavailableException(
            DomainKey domainKey,
            DomainVersion domainVersion,
            string message) : base(message)
        {
            DomainKey = domainKey;
            DomainVersion = domainVersion;
        }
    }
}
