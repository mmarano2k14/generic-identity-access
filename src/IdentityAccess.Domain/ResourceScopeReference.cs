

namespace IdentityAccess.Domain
{

    /// <summary>A stable generic resource-scope identity inside one tenant and application.</summary>
    public sealed record ResourceScopeReference
    {
        /// <summary>Gets the tenant.</summary>
        public TenantReference Tenant { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the resource scope identifier.</summary>
        public Guid ResourceScopeId { get; }

        /// <summary>Initializes a new instance of <see cref="ResourceScopeReference"/>.</summary>
        public ResourceScopeReference(TenantReference tenant, ApplicationKey application, Guid resourceScopeId)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(application);
            Tenant = tenant;
            Application = application;
            ResourceScopeId = ModelGuard.Identifier(resourceScopeId, nameof(resourceScopeId));
        }
    }
}
