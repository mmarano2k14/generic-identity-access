namespace IdentityAccess.Contracts;

public sealed record GroupProfileResponse(Guid IdentityScopeId, Guid TenantId, string ApplicationKey, Guid GroupId, string DisplayName, string Status);
