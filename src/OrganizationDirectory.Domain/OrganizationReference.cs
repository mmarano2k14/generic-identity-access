namespace OrganizationDirectory.Domain
{
    /// <summary>Tenant-scoped organization identity.</summary>
    public sealed record OrganizationReference
    {
        /// <summary>Gets the identity scope.</summary>
        public IdentityScopeId IdentityScopeId { get; }
        /// <summary>Gets the tenant.</summary>
        public TenantId TenantId { get; }
        /// <summary>Gets the organization id.</summary>
        public OrganizationId OrganizationId { get; }

        /// <summary>Initializes an organization reference.</summary>
        public OrganizationReference(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            OrganizationId organizationId)
        {
            IdentityScopeId = identityScopeId;
            TenantId = tenantId;
            OrganizationId = organizationId;
        }
    }
}
