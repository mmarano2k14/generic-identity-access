namespace IdentityAccess.Domain
{
    /// <summary>A stable application-scoped key for a reusable managed policy definition.</summary>
    public sealed record ManagedPolicyKey
    {
        private const string InvalidKeyMessage =
            "Managed policy keys use 1 to 128 lowercase letters, digits or hyphens, starting with a letter.";

        /// <summary>Gets the canonical managed policy key.</summary>
        public string Value { get; }

        /// <summary>Initializes a new instance of <see cref="ManagedPolicyKey"/>.</summary>
        public ManagedPolicyKey(string value)
        {
            Value = KeySyntax.RequireCanonicalLowercaseSlug(
                value,
                nameof(value),
                maximumLength: 128,
                allowUnderscore: false,
                InvalidKeyMessage);
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
