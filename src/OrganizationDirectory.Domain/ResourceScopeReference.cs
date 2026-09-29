namespace OrganizationDirectory.Domain
{
    /// <summary>Application-aware reference to an external authorization resource scope.</summary>
    public sealed record ResourceScopeReference
    {
        /// <summary>Gets the identity scope.</summary>
        public IdentityScopeId IdentityScopeId { get; }
        /// <summary>Gets the tenant.</summary>
        public TenantId TenantId { get; }
        /// <summary>Gets the application key.</summary>
        public ApplicationKey ApplicationKey { get; }
        /// <summary>Gets the resource-scope identifier.</summary>
        public ResourceScopeId ResourceScopeId { get; }
        /// <summary>Gets the external scope type.</summary>
        public ScopeType ScopeType { get; }
        /// <summary>Gets the external security-model version.</summary>
        public SecurityModelVersion ModelVersion { get; }

        /// <summary>Initializes an external resource-scope reference.</summary>
        public ResourceScopeReference(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            ApplicationKey applicationKey,
            ResourceScopeId resourceScopeId,
            ScopeType scopeType,
            SecurityModelVersion modelVersion)
        {
            ArgumentNullException.ThrowIfNull(applicationKey);
            ArgumentNullException.ThrowIfNull(scopeType);
            IdentityScopeId = identityScopeId;
            TenantId = tenantId;
            ApplicationKey = applicationKey;
            ResourceScopeId = resourceScopeId;
            ScopeType = scopeType;
            ModelVersion = modelVersion;
        }
    }
}
