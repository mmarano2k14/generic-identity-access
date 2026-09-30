namespace OrganisationProfile.Domain
{
    /// <summary>Stable domain key such as commerce, inventory, or finance.</summary>
    public readonly record struct DomainKey
    {
        /// <summary>Gets the normalized domain key.</summary>
        public string Value { get; }

        /// <summary>Initializes a validated domain key.</summary>
        public DomainKey(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var normalized = value.Trim();

            if (!StableProfileKeyRules.IsValid(normalized))
            {
                throw new ArgumentException(
                    "Domain key must match ^[a-z][a-z0-9-]{0,63}$.",
                    nameof(value));
            }

            Value = normalized;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
