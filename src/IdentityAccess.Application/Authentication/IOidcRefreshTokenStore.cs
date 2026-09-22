using IdentityAccess.Application.Routing;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Persists SHA-256 refresh-token hashes and atomically rotates or revokes refresh-token
    /// families against current local-session and user state.
    /// </summary>
    public interface IOidcRefreshTokenStore
    {
        /// <summary>
        /// Creates the initial family member only while the source session and current user remain
        /// eligible. Only the supplied SHA-256 token hash is persisted.
        /// </summary>
        Task<bool> CreateFamilyForActiveSessionAsync(
            ResolvedDatabaseRoute route,
            OidcRefreshTokenGrant grant,
            byte[] tokenHash,
            CancellationToken cancellationToken);

        /// <summary>
        /// Atomically inspects and rotates a valid current token, or revokes the whole family when
        /// a previously consumed token is replayed.
        /// </summary>
        Task<OidcRefreshTokenRotationResult> RotateAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            byte[] presentedTokenHash,
            Guid replacementTokenId,
            byte[] replacementTokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken);
    }
}
