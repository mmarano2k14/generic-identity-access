using System.Text.RegularExpressions;

namespace OrganizationDirectory.Domain
{
    /// <summary>Application identifier used to disambiguate external security-model linkage.</summary>
    public sealed record ApplicationKey
    {
        private static readonly Regex Syntax = new(
            "^[a-z][a-z0-9-]{0,63}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>Gets the canonical application key.</summary>
        public string Value { get; }

        /// <summary>Initializes a canonical lowercase application key.</summary>
        public ApplicationKey(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!Syntax.IsMatch(value))
                throw new ArgumentException(
                    "Application keys use 1 to 64 lowercase letters, digits or hyphens and start with a letter.",
                    nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
