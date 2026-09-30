namespace OrganisationProfile.Domain
{
    /// <summary>Lifecycle state of a reusable OrganisationProfile template definition.</summary>
    public enum OrganisationProfileTemplateStatus : short
    {
        /// <summary>The template may be used for new profile composition.</summary>
        Active = 1,

        /// <summary>The template remains addressable but may not be newly selected.</summary>
        Disabled = 2
    }
}
