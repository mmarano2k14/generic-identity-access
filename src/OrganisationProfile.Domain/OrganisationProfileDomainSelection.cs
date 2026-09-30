namespace OrganisationProfile.Domain
{
    /// <summary>One effective, explicitly version-pinned domain.</summary>
    public sealed record OrganisationProfileDomainSelection
    {
        /// <summary>Gets the domain key.</summary>
        public DomainKey DomainKey { get; }

        /// <summary>Gets the pinned domain version.</summary>
        public DomainVersion DomainVersion { get; }

        /// <summary>Initializes one domain/version pair.</summary>
        public OrganisationProfileDomainSelection(
            DomainKey domainKey,
            DomainVersion domainVersion)
        {
            DomainKey = domainKey;
            DomainVersion = domainVersion;
        }
    }
}
