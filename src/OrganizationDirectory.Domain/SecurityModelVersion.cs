namespace OrganizationDirectory.Domain
{
    /// <summary>Positive application security-model version.</summary>
    public readonly record struct SecurityModelVersion
    {
        /// <summary>Gets the version number.</summary>
        public int Value { get; }

        /// <summary>Initializes a positive security-model version.</summary>
        public SecurityModelVersion(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Security model version must be positive.");
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
