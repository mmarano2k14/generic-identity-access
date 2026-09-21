namespace IdentityAccess.Domain;

/// <summary>Stable account identity within a logical identity scope. No database identifier.</summary>
public sealed record SubjectReference
{
    public Guid IdentityScopeId { get; }
    public Guid UserId { get; }

    public SubjectReference(Guid identityScopeId, Guid userId)
    {
        IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
        UserId = ModelGuard.Identifier(userId, nameof(userId));
    }
}
