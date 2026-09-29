namespace OrganizationDirectory.Domain
{
    /// <summary>Explicit organizational belonging for one tenant membership.</summary>
    public sealed class OrganizationMembership
    {
        /// <summary>Gets the organization.</summary>
        public OrganizationReference Organization { get; }
        /// <summary>Gets the external tenant membership.</summary>
        public TenantMembershipReference TenantMembership { get; }
        /// <summary>Gets membership status.</summary>
        public OrganizationMembershipStatus Status { get; }
        /// <summary>Gets optimistic concurrency version.</summary>
        public long RowVersion { get; }
        /// <summary>Gets creation time.</summary>
        public DateTimeOffset CreatedAt { get; }
        /// <summary>Gets last update time.</summary>
        public DateTimeOffset UpdatedAt { get; }

        /// <summary>Initializes organization membership without granting authorization semantics.</summary>
        public OrganizationMembership(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            OrganizationMembershipStatus status,
            long rowVersion,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(tenantMembership);
            if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (rowVersion < 0) throw new ArgumentOutOfRangeException(nameof(rowVersion));
            if (createdAt == default) throw new ArgumentException("Created time is required.", nameof(createdAt));
            if (updatedAt < createdAt) throw new ArgumentException("Updated time cannot precede creation time.", nameof(updatedAt));
            if (organization.IdentityScopeId != tenantMembership.IdentityScopeId || organization.TenantId != tenantMembership.TenantId)
                throw new ArgumentException("Organization membership cannot cross identity-scope or tenant boundaries.", nameof(tenantMembership));

            Organization = organization;
            TenantMembership = tenantMembership;
            Status = status;
            RowVersion = rowVersion;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
    }
}
