namespace OrganisationProfile.Domain
{
    /// <summary>
    /// References Organization identity owned by Generic Organization Directory without duplicating
    /// Organization state or business semantics.
    /// </summary>
    public sealed record OrganizationReference
    {
        /// <summary>Gets the owning Identity Scope.</summary>
        public Guid IdentityScopeId { get; }

        /// <summary>Gets the owning Tenant.</summary>
        public Guid TenantId { get; }

        /// <summary>Gets the referenced Organization.</summary>
        public Guid OrganizationId { get; }

        /// <summary>Initializes a complete external Organization reference.</summary>
        public OrganizationReference(
            Guid identityScopeId,
            Guid tenantId,
            Guid organizationId)
        {
            if (identityScopeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "IdentityScopeId cannot be empty.",
                    nameof(identityScopeId));
            }

            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException(
                    "TenantId cannot be empty.",
                    nameof(tenantId));
            }

            if (organizationId == Guid.Empty)
            {
                throw new ArgumentException(
                    "OrganizationId cannot be empty.",
                    nameof(organizationId));
            }

            IdentityScopeId = identityScopeId;
            TenantId = tenantId;
            OrganizationId = organizationId;
        }
    }
}
