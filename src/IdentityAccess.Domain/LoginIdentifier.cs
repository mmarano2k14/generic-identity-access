using System.Text;

namespace IdentityAccess.Domain
{

    /// <summary>
    /// Login identifier inside one identity scope. It is not the immutable user identity.
    /// Normalization is deterministic and used only for credential lookup/uniqueness.
    /// </summary>
    public sealed record LoginIdentifier
    {
        /// <summary>Defines the maximum length constant.</summary>
        public const int MaximumLength = 320;

        /// <summary>Gets the original validated login identifier.</summary>
        public string Value { get; }
        /// <summary>Gets the normalized login identifier used for equality and lookup.</summary>
        public string NormalizedValue { get; }

        /// <summary>Initializes a new instance of <see cref="LoginIdentifier"/>.</summary>
        public LoginIdentifier(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            var trimmed = value.Trim();
            if (trimmed.Length > MaximumLength || trimmed.Any(char.IsControl))
                throw new ArgumentException("A login identifier must contain 1 to 320 printable characters.", nameof(value));

            Value = trimmed;
            NormalizedValue = trimmed.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
