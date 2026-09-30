namespace OrganisationProfile.Domain
{
    /// <summary>
    /// Immutable published template version containing explicitly pinned domain versions.
    /// </summary>
    public sealed class PublishedOrganisationProfileTemplateVersion
    {
        /// <summary>Gets the template key.</summary>
        public OrganisationProfileTemplateKey TemplateKey { get; }

        /// <summary>Gets the immutable template version.</summary>
        public OrganisationProfileTemplateVersionNumber Version { get; }

        /// <summary>Gets the deterministic, key-ordered domain composition.</summary>
        public IReadOnlyList<OrganisationProfileDomainSelection> Domains { get; }

        /// <summary>Gets the deterministic published content hash.</summary>
        public OrganisationProfileContentHash ContentHash { get; }

        /// <summary>Gets publication time.</summary>
        public DateTimeOffset PublishedAt { get; }

        /// <summary>Initializes immutable published template content.</summary>
        public PublishedOrganisationProfileTemplateVersion(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            OrganisationProfileContentHash contentHash,
            DateTimeOffset publishedAt)
        {
            ArgumentNullException.ThrowIfNull(domains);

            var ordered = domains
                .OrderBy(selection => selection.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (ordered
                .GroupBy(selection => selection.DomainKey)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "A published template version cannot contain duplicate DomainKey entries.",
                    nameof(domains));
            }

            TemplateKey = templateKey;
            Version = version;
            Domains = ordered;
            ContentHash = contentHash;
            PublishedAt = publishedAt;
        }
    }
}
