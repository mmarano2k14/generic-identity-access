namespace OrganizationDirectory.Domain
{
    /// <summary>Application-aware linkage between an organization and an external authorization scope.</summary>
    public sealed class OrganizationResourceScopeLink
    {
        /// <summary>Gets the organization.</summary>
        public OrganizationReference Organization { get; }
        /// <summary>Gets the external resource scope.</summary>
        public ResourceScopeReference ResourceScope { get; }
        /// <summary>Gets link status.</summary>
        public OrganizationResourceScopeLinkStatus Status { get; }
        /// <summary>Gets optimistic concurrency version.</summary>
        public long RowVersion { get; }
        /// <summary>Gets creation time.</summary>
        public DateTimeOffset CreatedAt { get; }
        /// <summary>Gets last update time.</summary>
        public DateTimeOffset UpdatedAt { get; }

        /// <summary>Initializes an organization-to-resource-scope linkage.</summary>
        public OrganizationResourceScopeLink(
            OrganizationReference organization,
            ResourceScopeReference resourceScope,
            OrganizationResourceScopeLinkStatus status,
            long rowVersion,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(resourceScope);
            if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (rowVersion < 0) throw new ArgumentOutOfRangeException(nameof(rowVersion));
            if (createdAt == default) throw new ArgumentException("Created time is required.", nameof(createdAt));
            if (updatedAt < createdAt) throw new ArgumentException("Updated time cannot precede creation time.", nameof(updatedAt));
            if (organization.IdentityScopeId != resourceScope.IdentityScopeId || organization.TenantId != resourceScope.TenantId)
                throw new ArgumentException("Resource-scope linkage cannot cross identity-scope or tenant boundaries.", nameof(resourceScope));

            Organization = organization;
            ResourceScope = resourceScope;
            Status = status;
            RowVersion = rowVersion;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
    }
}
