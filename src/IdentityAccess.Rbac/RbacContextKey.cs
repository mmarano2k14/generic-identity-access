namespace IdentityAccess.Rbac
{
    /// <summary>
    /// Canonical external RBAC project or namespace segment.
    /// </summary>
    /// <remarks>
    /// The value is trimmed and normalized to lowercase. The segment is bounded to
    /// 128 characters and cannot contain TRN separators or wildcard markers.
    /// </remarks>
    public sealed record RbacContextKey
    {
        /// <summary>Gets the canonical RBAC context segment.</summary>
        public string Value { get; }

        /// <summary>Initializes a new instance of <see cref="RbacContextKey"/>.</summary>
        /// <param name="value">Project or namespace segment.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the value is empty, exceeds 128 characters, or contains ':' or '*'.
        /// </exception>
        public RbacContextKey(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var canonical = value.Trim().ToLowerInvariant();
            if (canonical.Length > 128 ||
                canonical.Contains(':', StringComparison.Ordinal) ||
                canonical.Contains('*', StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "RBAC project and namespace must be concrete context segments of at most 128 characters.",
                    nameof(value));
            }

            Value = canonical;
        }

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
