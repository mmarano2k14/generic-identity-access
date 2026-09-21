namespace IdentityAccess.Domain;

public sealed record TenantReference
{
    public Guid IdentityScopeId { get; }
    public Guid TenantId { get; }

    public TenantReference(Guid identityScopeId, Guid tenantId)
    {
        IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
        TenantId = ModelGuard.Identifier(tenantId, nameof(tenantId));
    }
}
