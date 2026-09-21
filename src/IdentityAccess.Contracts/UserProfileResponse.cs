namespace IdentityAccess.Contracts;

public sealed record UserProfileResponse(Guid IdentityScopeId, Guid UserId, string DisplayName, string Status);
