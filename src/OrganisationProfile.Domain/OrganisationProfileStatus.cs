namespace OrganisationProfile.Domain
{
    /// <summary>Lifecycle state of one OrganisationProfile.</summary>
    public enum OrganisationProfileStatus : short
    {
        /// <summary>The profile participates in normal profile-aware operations.</summary>
        Active = 1,

        /// <summary>The profile is retained but excluded from normal operations.</summary>
        Disabled = 2
    }
}
