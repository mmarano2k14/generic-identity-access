namespace OrganisationProfile.Domain
{
    /// <summary>Lifecycle state of one OrganisationProfile template version.</summary>
    public enum OrganisationProfileTemplateVersionStatus : short
    {
        /// <summary>The version may still be edited before publication.</summary>
        Draft = 1,

        /// <summary>The version is immutable and available for deterministic pinning.</summary>
        Published = 2,

        /// <summary>The immutable historical version is no longer offered for new assignment.</summary>
        Retired = 3
    }
}
