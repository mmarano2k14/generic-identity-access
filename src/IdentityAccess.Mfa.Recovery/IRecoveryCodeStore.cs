using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Provider-owned persistence boundary for recovery-code hashes and single-use consumption.</summary>
    internal interface IRecoveryCodeStore
    {
        Task<bool> ReplaceActiveSetAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            IReadOnlyList<byte[]> codeHashes,
            CancellationToken cancellationToken);

        Task<RecoveryCodeStoreMutationResult> TryConsumeAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            byte[] codeHash,
            DateTimeOffset consumedAt,
            CancellationToken cancellationToken);

        Task<RecoveryPasswordResetStoreResult> TryResetPasswordAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            byte[] codeHash,
            string passwordHash,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken);
    }
}
