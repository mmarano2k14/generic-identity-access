

namespace IdentityAccess.Domain
{

    /// <summary>Identifies a tenant independently of physical database placement.</summary>
    public sealed record TenantReference
    {
        /// <summary>Gets the identity scope identifier.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the tenant identifier.</summary>
        public Guid TenantId { get; }

        /// <summary>Initializes a new instance of <see cref="TenantReference"/>.</summary>
        public TenantReference(Guid identityScopeId, Guid tenantId)
        {
            IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
            TenantId = ModelGuard.Identifier(tenantId, nameof(tenantId));
        }
    }
}
