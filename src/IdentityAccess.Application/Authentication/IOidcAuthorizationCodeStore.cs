using IdentityAccess.Application.Routing;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>Persists and atomically consumes opaque OpenID Connect authorization codes.</summary>
    public interface IOidcAuthorizationCodeStore
    {
        /// <summary>
        /// Creates a code record only while the source session remains active and its current user
        /// remains active. Only the SHA-256 code hash is persisted.
        /// </summary>
        Task<bool> CreateForActiveSessionAsync(
            ResolvedDatabaseRoute route,
            OidcAuthorizationCodeGrant grant,
            byte[] codeHash,
            CancellationToken cancellationToken);

        /// <summary>
        /// Atomically consumes an unexpired code only when client, redirect URI, PKCE challenge,
        /// active session, and current active-user state still match.
        /// </summary>
        Task<OidcAuthorizationCodeGrant?> ConsumeAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            string redirectUri,
            byte[] codeHash,
            string expectedCodeChallenge,
            DateTimeOffset now,
            CancellationToken cancellationToken);
    }
}
