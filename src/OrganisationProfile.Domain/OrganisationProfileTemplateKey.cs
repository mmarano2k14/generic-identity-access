namespace OrganisationProfile.Domain
{
    /// <summary>Stable reusable template key such as ecommerce-standard.</summary>
    public readonly record struct OrganisationProfileTemplateKey
    {
        /// <summary>Gets the normalized stable key.</summary>
        public string Value { get; }

        /// <summary>Initializes a validated lowercase stable key.</summary>
        public OrganisationProfileTemplateKey(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var normalized = value.Trim();

            if (!StableProfileKeyRules.IsValid(normalized))
            {
                throw new ArgumentException(
                    "Template key must match ^[a-z][a-z0-9-]{0,63}$.",
                    nameof(value));
            }

            Value = normalized;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
