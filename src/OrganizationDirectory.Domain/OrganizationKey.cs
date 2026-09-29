using System.Text.RegularExpressions;

namespace OrganizationDirectory.Domain
{
    /// <summary>Tenant-local, human-readable stable organization key.</summary>
    public sealed record OrganizationKey
    {
        private static readonly Regex Syntax = new(
            "^[a-z][a-z0-9-]{0,63}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>Gets the canonical key.</summary>
        public string Value { get; }

        /// <summary>Initializes a canonical lowercase organization key.</summary>
        public OrganizationKey(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!Syntax.IsMatch(value))
                throw new ArgumentException(
                    "Organization keys use 1 to 64 lowercase letters, digits or hyphens and start with a letter.",
                    nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
