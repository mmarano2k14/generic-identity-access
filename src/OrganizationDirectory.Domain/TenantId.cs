namespace OrganizationDirectory.Domain
{
    /// <summary>Stable tenant reference owned by the external identity system.</summary>
    public readonly record struct TenantId
    {
        /// <summary>Gets the underlying identifier.</summary>
        public Guid Value { get; }

        /// <summary>Initializes a non-empty tenant identifier.</summary>
        public TenantId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("Tenant id cannot be empty.", nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString("D");
    }
}
