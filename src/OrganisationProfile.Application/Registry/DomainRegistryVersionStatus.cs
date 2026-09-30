namespace OrganisationProfile.Application.Registry
{
    /// <summary>Lifecycle state exposed by the external Domain Registry boundary.</summary>
    public enum DomainRegistryVersionStatus : short
    {
        /// <summary>The version may be selected for new profile/template composition.</summary>
        Published = 1,

        /// <summary>The historical version remains resolvable but may not be newly selected.</summary>
        Retired = 2
    }
}
