namespace OrganisationProfile.Domain
{
    /// <summary>
    /// module-owned semantic configuration attached to exactly one external Organization identity.
    /// </summary>
    public sealed class OrganisationProfile
    {
        /// <summary>Gets the stable profile identity.</summary>
        public OrganisationProfileId OrganisationProfileId { get; }

        /// <summary>Gets the Organization owned by Generic Organization Directory.</summary>
        public OrganizationReference Organization { get; }

        /// <summary>Gets the optional immutable profile-template pin.</summary>
        public OrganisationProfileTemplatePin? TemplatePin { get; }

        /// <summary>Gets lifecycle status.</summary>
        public OrganisationProfileStatus Status { get; }

        /// <summary>Gets optimistic-concurrency row version. Zero denotes a not-yet-persisted instance.</summary>
        public long RowVersion { get; }

        /// <summary>Gets creation time.</summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>Gets last semantic update time.</summary>
        public DateTimeOffset UpdatedAt { get; }

        /// <summary>Initializes one OrganisationProfile state snapshot.</summary>
        public OrganisationProfile(
            OrganisationProfileId organisationProfileId,
            OrganizationReference organization,
            OrganisationProfileTemplatePin? templatePin,
            OrganisationProfileStatus status,
            long rowVersion,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            ArgumentNullException.ThrowIfNull(organization);

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            if (rowVersion < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rowVersion),
                    "RowVersion cannot be negative.");
            }

            if (updatedAt < createdAt)
            {
                throw new ArgumentException(
                    "UpdatedAt cannot precede CreatedAt.",
                    nameof(updatedAt));
            }

            OrganisationProfileId = organisationProfileId;
            Organization = organization;
            TemplatePin = templatePin;
            Status = status;
            RowVersion = rowVersion;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>Creates one active not-yet-persisted profile.</summary>
        public static OrganisationProfile Create(
            OrganisationProfileId organisationProfileId,
            OrganizationReference organization,
            OrganisationProfileTemplatePin? templatePin,
            DateTimeOffset now) =>
            new(
                organisationProfileId,
                organization,
                templatePin,
                OrganisationProfileStatus.Active,
                rowVersion: 0,
                createdAt: now,
                updatedAt: now);

        /// <summary>Returns a new state snapshot with a different template pin.</summary>
        public OrganisationProfile WithTemplate(
            OrganisationProfileTemplatePin? templatePin,
            DateTimeOffset updatedAt) =>
            new(
                OrganisationProfileId,
                Organization,
                templatePin,
                Status,
                RowVersion,
                CreatedAt,
                updatedAt);

        /// <summary>Returns a new state snapshot with a different lifecycle status.</summary>
        public OrganisationProfile WithStatus(
            OrganisationProfileStatus status,
            DateTimeOffset updatedAt) =>
            new(
                OrganisationProfileId,
                Organization,
                TemplatePin,
                status,
                RowVersion,
                CreatedAt,
                updatedAt);
    }
}
