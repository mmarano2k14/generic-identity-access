using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Represents credential metadata.</summary>
    public sealed record CredentialMetadata(Guid UserId, string LoginIdentifier, int FailedAccessCount,
        DateTimeOffset? LockoutUntil, long Version);
}
