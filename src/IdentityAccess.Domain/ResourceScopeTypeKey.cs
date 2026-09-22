namespace IdentityAccess.Domain
{
    /// <summary>A generic application-defined resource-scope type key such as organization, business or project.</summary>
    public sealed record ResourceScopeTypeKey
    {
        private const string InvalidKeyMessage =
            "Resource scope type keys use 1 to 64 lowercase letters, digits or hyphens, starting with a letter.";

        /// <summary>Gets the canonical resource scope type key.</summary>
        public string Value { get; }

        /// <summary>Initializes a new instance of <see cref="ResourceScopeTypeKey"/>.</summary>
        public ResourceScopeTypeKey(string value)
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
