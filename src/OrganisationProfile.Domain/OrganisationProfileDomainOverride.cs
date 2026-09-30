namespace OrganisationProfile.Domain
{
    /// <summary>Organization-specific deterministic override of one template domain.</summary>
    public sealed record OrganisationProfileDomainOverride
    {
        /// <summary>Gets the target domain key.</summary>
        public DomainKey DomainKey { get; }

        /// <summary>
        /// Gets the required version for Enable, or null for Disable.
        /// </summary>
        public DomainVersion? DomainVersion { get; }

        /// <summary>Gets the override operation.</summary>
        public OrganisationProfileDomainOverrideOperation Operation { get; }

        /// <summary>Initializes a validated domain override.</summary>
        public OrganisationProfileDomainOverride(
            DomainKey domainKey,
            DomainVersion? domainVersion,
            OrganisationProfileDomainOverrideOperation operation)
        {
            if (!Enum.IsDefined(operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }

            if (operation == OrganisationProfileDomainOverrideOperation.Enable &&
                domainVersion is null)
            {
                throw new ArgumentException(
                    "Enable override requires an explicit DomainVersion.",
                    nameof(domainVersion));
            }

            if (operation == OrganisationProfileDomainOverrideOperation.Disable &&
                domainVersion is not null)
            {
                throw new ArgumentException(
                    "Disable override must not carry a DomainVersion.",
                    nameof(domainVersion));
            }

            DomainKey = domainKey;
            DomainVersion = domainVersion;
            Operation = operation;
        }
    }
}
