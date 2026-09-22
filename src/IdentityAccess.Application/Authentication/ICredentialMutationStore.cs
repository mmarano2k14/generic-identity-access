using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Performs credential mutations atomically with persisted account and session invariants.
    /// </summary>
    public interface ICredentialMutationStore
    {
        /// <summary>
        /// Creates a credential only when the referenced user exists in the same resolved route.
        /// </summary>
        Task<VersionedRecord<PasswordCredential>> CreateForExistingUserAsync(
            ResolvedDatabaseRoute route,
            PasswordCredential credential,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates a password only when the referenced user exists and the expected credential
        /// version matches, then revokes every active session for the subject in the same database
        /// statement.
        /// </summary>
        Task<VersionedRecord<PasswordCredential>> UpdatePasswordAndRevokeSessionsAsync(
            ResolvedDatabaseRoute route,
            PasswordCredential credential,
            long expectedVersion,
            CancellationToken cancellationToken);
    }
}
