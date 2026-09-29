namespace OrganizationDirectory.Domain
{
    /// <summary>Stable resource-scope reference owned by the external identity system.</summary>
    public readonly record struct ResourceScopeId
    {
        /// <summary>Gets the underlying identifier.</summary>
        public Guid Value { get; }

        /// <summary>Initializes a non-empty resource-scope identifier.</summary>
        public ResourceScopeId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("Resource scope id cannot be empty.", nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString("D");
    }
}
