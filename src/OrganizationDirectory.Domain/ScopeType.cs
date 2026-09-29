using System.Text.RegularExpressions;

namespace OrganizationDirectory.Domain
{
    /// <summary>External resource-scope type declared by an application security model.</summary>
    public sealed record ScopeType
    {
        private static readonly Regex Syntax = new(
            "^[a-z][a-z0-9-]{0,63}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>Gets the canonical scope type.</summary>
        public string Value { get; }

        /// <summary>Initializes a canonical lowercase scope type.</summary>
        public ScopeType(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!Syntax.IsMatch(value))
                throw new ArgumentException(
                    "Scope types use 1 to 64 lowercase letters, digits or hyphens and start with a letter.",
                    nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
