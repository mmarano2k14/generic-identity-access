namespace OrganizationDirectory.Domain
{
    /// <summary>Stable tenant-membership reference owned by the external identity system.</summary>
    public readonly record struct TenantMembershipId
    {
        /// <summary>Gets the underlying identifier.</summary>
        public Guid Value { get; }

        /// <summary>Initializes a non-empty tenant-membership identifier.</summary>
        public TenantMembershipId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("Tenant membership id cannot be empty.", nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString("D");
    }
}
