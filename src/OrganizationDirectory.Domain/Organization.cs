namespace OrganizationDirectory.Domain
{
    /// <summary>Immutable organization state within one identity scope and tenant.</summary>
    public sealed class Organization
    {
        /// <summary>Gets the organization reference.</summary>
        public OrganizationReference Reference { get; }
        /// <summary>Gets the stable tenant-local key.</summary>
        public OrganizationKey Key { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the application-defined organization type.</summary>
        public OrganizationType Type { get; }
        /// <summary>Gets the optional tenant-local parent.</summary>
        public OrganizationReference? Parent { get; }
        /// <summary>Gets the lifecycle status.</summary>
        public OrganizationStatus Status { get; }
        /// <summary>Gets the optimistic concurrency version.</summary>
        public long RowVersion { get; }
        /// <summary>Gets creation time.</summary>
        public DateTimeOffset CreatedAt { get; }
        /// <summary>Gets last update time.</summary>
        public DateTimeOffset UpdatedAt { get; }

        /// <summary>Initializes immutable organization state.</summary>
        public Organization(
            OrganizationReference reference,
            OrganizationKey key,
            string displayName,
            OrganizationType type,
            OrganizationReference? parent,
            OrganizationStatus status,
            long rowVersion,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (rowVersion < 0) throw new ArgumentOutOfRangeException(nameof(rowVersion));
            if (createdAt == default) throw new ArgumentException("Created time is required.", nameof(createdAt));
            if (updatedAt < createdAt) throw new ArgumentException("Updated time cannot precede creation time.", nameof(updatedAt));

            if (parent is not null)
            {
                if (parent.IdentityScopeId != reference.IdentityScopeId || parent.TenantId != reference.TenantId)
                    throw new ArgumentException("Organization parent must remain in the same identity scope and tenant.", nameof(parent));
                if (parent.OrganizationId == reference.OrganizationId)
                    throw new ArgumentException("Organization cannot parent itself.", nameof(parent));
            }

            Reference = reference;
            Key = key;
            DisplayName = displayName.Trim();
            Type = type;
            Parent = parent;
            Status = status;
            RowVersion = rowVersion;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
    }
}
