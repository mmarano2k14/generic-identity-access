namespace OrganisationProfile.Domain
{
    /// <summary>Lowercase hexadecimal SHA-256 identity of deterministic profile content.</summary>
    public readonly record struct OrganisationProfileContentHash
    {
        /// <summary>Gets the normalized 64-character lowercase SHA-256 value.</summary>
        public string Value { get; }

        /// <summary>Initializes a validated SHA-256 content hash.</summary>
        public OrganisationProfileContentHash(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var normalized = value.Trim();

            if (normalized.Length != 64 ||
                normalized.Any(character =>
                    !((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f'))))
            {
                throw new ArgumentException(
                    "Content hash must be a 64-character lowercase hexadecimal SHA-256 value.",
                    nameof(value));
            }

            Value = normalized;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
