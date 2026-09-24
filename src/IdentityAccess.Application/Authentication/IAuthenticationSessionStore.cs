using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Defines persistence operations for local authentication sessions and their lifecycle.
    /// </summary>
    public interface IAuthenticationSessionStore
    {
        /// <summary>
        /// Creates a session only when the referenced user is currently active.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the session was created; otherwise
        /// <see langword="false"/> when the current user state does not permit session issuance.
        /// </returns>
        Task<bool> CreateForActiveUserAsync(
            ResolvedDatabaseRoute route,
            AuthenticationSession session,
            byte[] tokenHash,
            CancellationToken cancellationToken);

        /// <summary>
        /// Validates a session only when the session is active and the current user remains active.
        /// </summary>
        Task<AuthenticationSession?> ValidateAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            byte[] tokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken);

        /// <summary>
        /// Revalidates an already authenticated session reference against current session and user
        /// state without treating the reference itself as a credential.
        /// </summary>
        Task<AuthenticationSession?> ValidateReferenceAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            DateTimeOffset now,
            CancellationToken cancellationToken);

        /// <summary>
        /// Atomically upgrades one active session after an additional authentication factor has
        /// been verified for the exact subject/client/application binding.
        /// </summary>
        Task<AuthenticationSession?> UpgradeAssuranceAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            string factorMethodReference,
            DateTimeOffset verifiedAt,
            CancellationToken cancellationToken);

        /// <summary>Revokes one opaque local session.</summary>
        Task<bool> RevokeAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            byte[] tokenHash,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken);

        /// <summary>
        /// Revokes every active session for the requested subject across clients and applications
        /// within the resolved identity scope.
        /// </summary>
        Task<int> RevokeAllForSubjectAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken);

        /// <summary>
        /// Revokes every active session for the requested registered client within the resolved
        /// application route.
        /// </summary>
        Task<int> RevokeAllForClientAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken);
    }
}
