using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines the contract for password credential store.</summary>
    public interface IPasswordCredentialStore
    {
        /// <summary>Finds a password credential by normalized login identifier.</summary>
        Task<VersionedRecord<PasswordCredential>?> FindByLoginAsync(ResolvedDatabaseRoute route,
            string normalizedLoginIdentifier, CancellationToken cancellationToken);

        /// <summary>Gets a password credential by subject.</summary>
        Task<VersionedRecord<PasswordCredential>?> GetBySubjectAsync(ResolvedDatabaseRoute route,
            SubjectReference subject, CancellationToken cancellationToken);

        /// <summary>Creates a password credential record in the resolved database route.</summary>
        Task<VersionedRecord<PasswordCredential>> CreateAsync(ResolvedDatabaseRoute route,
            PasswordCredential credential, CancellationToken cancellationToken);

        /// <summary>Updates the password hash using optimistic concurrency.</summary>
        Task<VersionedRecord<PasswordCredential>> UpdatePasswordAsync(ResolvedDatabaseRoute route,
            PasswordCredential credential, long expectedVersion, CancellationToken cancellationToken);

        /// <summary>Records a failed password credential attempt and returns the updated persisted state.</summary>
        Task<VersionedRecord<PasswordCredential>> RecordFailureAsync(ResolvedDatabaseRoute route,
            SubjectReference subject, int lockoutThreshold, DateTimeOffset lockoutUntil,
            CancellationToken cancellationToken);

        /// <summary>Records a successful password credential operation and clears failure state as required.</summary>
        Task<VersionedRecord<PasswordCredential>> RecordSuccessAsync(ResolvedDatabaseRoute route,
            SubjectReference subject, CancellationToken cancellationToken);
    }
}
