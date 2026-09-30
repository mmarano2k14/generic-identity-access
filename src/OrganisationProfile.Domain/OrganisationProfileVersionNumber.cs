namespace OrganisationProfile.Domain
{
    /// <summary>
    /// Positive semantic snapshot version of one resolved OrganisationProfile.
    /// This is distinct from optimistic-concurrency RowVersion.
    /// </summary>
    public readonly record struct OrganisationProfileVersionNumber
    {
        /// <summary>Gets the positive semantic version number.</summary>
        public long Value { get; }

        /// <summary>Initializes a positive semantic version.</summary>
        public OrganisationProfileVersionNumber(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "OrganisationProfile semantic version must be positive.");
            }

            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() =>
            Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
