namespace OrganizationDirectory.Domain
{
    /// <summary>Stable identity-scope reference owned by the external identity system.</summary>
    public readonly record struct IdentityScopeId
    {
        /// <summary>Gets the underlying identifier.</summary>
        public Guid Value { get; }

        /// <summary>Initializes a non-empty identity-scope identifier.</summary>
        public IdentityScopeId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("Identity scope id cannot be empty.", nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString("D");
    }
}
