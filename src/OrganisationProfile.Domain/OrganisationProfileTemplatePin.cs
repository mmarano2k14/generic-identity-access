namespace OrganisationProfile.Domain
{
    /// <summary>Pins an OrganisationProfile to one immutable published template version.</summary>
    public sealed record OrganisationProfileTemplatePin
    {
        /// <summary>Gets the stable template key.</summary>
        public OrganisationProfileTemplateKey TemplateKey { get; }

        /// <summary>Gets the immutable template version.</summary>
        public OrganisationProfileTemplateVersionNumber Version { get; }

        /// <summary>Initializes one template-version pin.</summary>
        public OrganisationProfileTemplatePin(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version)
        {
            TemplateKey = templateKey;
            Version = version;
        }
    }
}
