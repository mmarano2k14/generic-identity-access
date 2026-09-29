namespace OrganizationDirectory.Domain
{
    /// <summary>Tenant-scoped reference to an external Identity Access tenant membership.</summary>
    public sealed record TenantMembershipReference
    {
        /// <summary>Gets the identity scope.</summary>
        public IdentityScopeId IdentityScopeId { get; }
        /// <summary>Gets the tenant.</summary>
        public TenantId TenantId { get; }
        /// <summary>Gets the tenant-membership identifier.</summary>
        public TenantMembershipId TenantMembershipId { get; }

        /// <summary>Initializes a tenant-membership reference.</summary>
        public TenantMembershipReference(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            TenantMembershipId tenantMembershipId)
        {
            IdentityScopeId = identityScopeId;
            TenantId = tenantId;
            TenantMembershipId = tenantMembershipId;
        }
    }
}
