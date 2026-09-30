namespace OrganisationProfile.Domain
{
    public sealed class OrganisationProfileTemplateVersion
    {
        public OrganisationProfileTemplateKey TemplateKey { get; }
        public OrganisationProfileTemplateVersionNumber Version { get; }
        public OrganisationProfileTemplateVersionStatus Status { get; }
        public IReadOnlyList<OrganisationProfileDomainSelection> Domains { get; }
        public OrganisationProfileContentHash? ContentHash { get; }
        public long RowVersion { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset UpdatedAt { get; }
        public DateTimeOffset? PublishedAt { get; }
        public DateTimeOffset? RetiredAt { get; }

        public OrganisationProfileTemplateVersion(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            OrganisationProfileTemplateVersionStatus status,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            OrganisationProfileContentHash? contentHash,
            long rowVersion,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt,
            DateTimeOffset? publishedAt,
            DateTimeOffset? retiredAt)
        {
            ArgumentNullException.ThrowIfNull(domains);

            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status));

            if (rowVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(rowVersion));

            if (updatedAt < createdAt)
                throw new ArgumentException(
                    "UpdatedAt cannot precede CreatedAt.",
                    nameof(updatedAt));

            var ordered = domains
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (ordered.GroupBy(item => item.DomainKey).Any(group => group.Count() > 1))
                throw new ArgumentException(
                    "Template version cannot contain duplicate DomainKey entries.",
                    nameof(domains));

            if (status == OrganisationProfileTemplateVersionStatus.Draft &&
                (contentHash is not null || publishedAt is not null || retiredAt is not null))
                throw new ArgumentException(
                    "Draft versions cannot carry publication metadata.");

            if (status == OrganisationProfileTemplateVersionStatus.Published &&
                (contentHash is null || publishedAt is null || retiredAt is not null))
                throw new ArgumentException(
                    "Published versions require hash and publication time only.");

            if (status == OrganisationProfileTemplateVersionStatus.Retired)
            {
                if (contentHash is null || publishedAt is null || retiredAt is null)
                    throw new ArgumentException(
                        "Retired versions require publication and retirement metadata.");

                if (retiredAt.Value < publishedAt.Value)
                    throw new ArgumentException(
                        "RetiredAt cannot precede PublishedAt.",
                        nameof(retiredAt));
            }

            TemplateKey = templateKey;
            Version = version;
            Status = status;
            Domains = ordered;
            ContentHash = contentHash;
            RowVersion = rowVersion;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            PublishedAt = publishedAt;
            RetiredAt = retiredAt;
        }

        public static OrganisationProfileTemplateVersion CreateDraft(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            DateTimeOffset now) =>
            new(
                templateKey,
                version,
                OrganisationProfileTemplateVersionStatus.Draft,
                domains,
                null,
                0,
                now,
                now,
                null,
                null);
    }
}
