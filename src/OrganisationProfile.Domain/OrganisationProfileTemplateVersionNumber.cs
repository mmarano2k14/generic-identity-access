namespace OrganisationProfile.Domain
{
    /// <summary>Positive immutable version number of one reusable profile template.</summary>
    public readonly record struct OrganisationProfileTemplateVersionNumber
    {
        /// <summary>Gets the positive version number.</summary>
        public int Value { get; }

        /// <summary>Initializes a positive template version.</summary>
        public OrganisationProfileTemplateVersionNumber(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Template version must be positive.");
            }

            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() =>
            Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
