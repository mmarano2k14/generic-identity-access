namespace OrganisationProfile.Domain
{
    /// <summary>Operation applied by an Organization-specific domain override.</summary>
    public enum OrganisationProfileDomainOverrideOperation : short
    {
        /// <summary>Enable or replace a domain with an explicitly pinned version.</summary>
        Enable = 1,

        /// <summary>Remove a domain inherited from the selected template composition.</summary>
        Disable = 2
    }
}
