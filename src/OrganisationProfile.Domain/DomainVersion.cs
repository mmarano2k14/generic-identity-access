namespace OrganisationProfile.Domain
{
    /// <summary>Positive immutable version pin for one domain.</summary>
    public readonly record struct DomainVersion
    {
        /// <summary>Gets the positive domain version.</summary>
        public int Value { get; }

        /// <summary>Initializes a positive domain version.</summary>
        public DomainVersion(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Domain version must be positive.");
            }

            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() =>
            Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
