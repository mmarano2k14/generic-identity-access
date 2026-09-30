namespace OrganisationProfile.Domain
{
    public sealed class OrganisationProfileTemplate
    {
        public OrganisationProfileTemplateKey TemplateKey { get; }
        public string DisplayName { get; }
        public OrganisationProfileTemplateStatus Status { get; }
        public long RowVersion { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset UpdatedAt { get; }

        public OrganisationProfileTemplate(
            OrganisationProfileTemplateKey templateKey,
            string displayName,
            OrganisationProfileTemplateStatus status,
            long rowVersion,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            var normalized = displayName.Trim();

            if (normalized.Length > 200)
                throw new ArgumentOutOfRangeException(nameof(displayName));

            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status));

            if (rowVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(rowVersion));

            if (updatedAt < createdAt)
                throw new ArgumentException(
                    "UpdatedAt cannot precede CreatedAt.",
                    nameof(updatedAt));

            TemplateKey = templateKey;
            DisplayName = normalized;
            Status = status;
            RowVersion = rowVersion;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static OrganisationProfileTemplate Create(
            OrganisationProfileTemplateKey templateKey,
            string displayName,
            DateTimeOffset now) =>
            new(
                templateKey,
                displayName,
                OrganisationProfileTemplateStatus.Active,
                0,
                now,
                now);
    }
}
