namespace OrganisationProfile.Domain
{
    /// <summary>
    /// Deterministically resolved OrganisationProfile used by consumer runtime/business subsystems.
    /// This is derived state, never an independently editable source of truth.
    /// </summary>
    public sealed class EffectiveOrganisationProfile
    {
        /// <summary>Gets the profile identity.</summary>
        public OrganisationProfileId OrganisationProfileId { get; }

        /// <summary>Gets the external Organization reference.</summary>
        public OrganizationReference Organization { get; }

        /// <summary>Gets the immutable semantic snapshot version.</summary>
        public OrganisationProfileVersionNumber Version { get; }

        /// <summary>Gets the optional originating template pin.</summary>
        public OrganisationProfileTemplatePin? TemplatePin { get; }

        /// <summary>Gets deterministic key-ordered, explicitly version-pinned domains.</summary>
        public IReadOnlyList<OrganisationProfileDomainSelection> Domains { get; }

        /// <summary>Gets deterministic composition hash.</summary>
        public OrganisationProfileContentHash ContentHash { get; }

        /// <summary>Gets resolution time.</summary>
        public DateTimeOffset ResolvedAt { get; }

        /// <summary>Initializes one deterministic effective profile.</summary>
        public EffectiveOrganisationProfile(
            OrganisationProfileId organisationProfileId,
            OrganizationReference organization,
            OrganisationProfileVersionNumber version,
            OrganisationProfileTemplatePin? templatePin,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            OrganisationProfileContentHash contentHash,
            DateTimeOffset resolvedAt)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(domains);

            var ordered = domains
                .OrderBy(selection => selection.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (ordered
                .GroupBy(selection => selection.DomainKey)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Effective profile cannot contain duplicate DomainKey entries.",
                    nameof(domains));
            }

            OrganisationProfileId = organisationProfileId;
            Organization = organization;
            Version = version;
            TemplatePin = templatePin;
            Domains = ordered;
            ContentHash = contentHash;
            ResolvedAt = resolvedAt;
        }
    }
}
