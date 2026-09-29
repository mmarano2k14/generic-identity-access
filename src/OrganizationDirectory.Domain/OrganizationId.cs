namespace OrganizationDirectory.Domain
{
    /// <summary>Stable organization identifier.</summary>
    public readonly record struct OrganizationId
    {
        /// <summary>Gets the underlying identifier.</summary>
        public Guid Value { get; }

        /// <summary>Initializes a non-empty organization identifier.</summary>
        public OrganizationId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("Organization id cannot be empty.", nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString("D");
    }
}
