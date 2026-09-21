namespace IdentityAccess.Contracts;

public sealed record TenantProfileResponse(Guid IdentityScopeId, Guid TenantId, string DisplayName, string Status);
