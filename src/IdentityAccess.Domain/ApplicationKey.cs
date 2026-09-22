namespace IdentityAccess.Domain
{
    /// <summary>An application identifier, not proof of trust or authorization.</summary>
    public sealed record ApplicationKey
    {
        private const string InvalidKeyMessage =
            "Application keys use 1 to 64 lowercase letters, digits or hyphens, starting with a letter.";

        /// <summary>Gets the canonical application key.</summary>
        public string Value { get; }

        /// <summary>Initializes a new instance of <see cref="ApplicationKey"/>.</summary>
        public ApplicationKey(string value)
        {
            Value = KeySyntax.RequireCanonicalLowercaseSlug(
                value,
                nameof(value),
                maximumLength: 64,
                allowUnderscore: false,
                InvalidKeyMessage);
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
