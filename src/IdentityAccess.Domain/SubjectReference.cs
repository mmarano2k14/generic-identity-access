

namespace IdentityAccess.Domain
{

    /// <summary>Stable account identity within a logical identity scope. No database identifier.</summary>
    public sealed record SubjectReference
    {
        /// <summary>Gets the identity scope identifier.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the user identifier.</summary>
        public Guid UserId { get; }

        /// <summary>Initializes a new instance of <see cref="SubjectReference"/>.</summary>
        public SubjectReference(Guid identityScopeId, Guid userId)
        {
            IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
            UserId = ModelGuard.Identifier(userId, nameof(userId));
        }
    }
}
