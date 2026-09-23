namespace IdentityAccess.Domain
{
    /// <summary>Stable generic key identifying one authentication-factor provider.</summary>
    public sealed record AuthenticationFactorProviderKey
    {
        private const string InvalidKeyMessage =
            "Authentication factor provider keys use 1 to 64 lowercase letters, digits or hyphens, starting with a letter.";

        /// <summary>Gets the canonical provider key.</summary>
        public string Value { get; }

        /// <summary>Initializes a new provider key.</summary>
        public AuthenticationFactorProviderKey(string value)
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
