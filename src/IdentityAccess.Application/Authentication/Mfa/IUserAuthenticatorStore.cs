using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Persists generic authenticator metadata while provider secrets remain provider-owned.</summary>
    public interface IUserAuthenticatorStore
    {
        /// <summary>Lists authenticators for one user.</summary>
        Task<IReadOnlyList<VersionedRecord<UserAuthenticator>>> ListByUserAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            CancellationToken cancellationToken);

        /// <summary>Gets one authenticator.</summary>
        Task<VersionedRecord<UserAuthenticator>?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid authenticatorId,
            CancellationToken cancellationToken);

        /// <summary>Creates generic metadata for a provider enrollment.</summary>
        Task<VersionedRecord<UserAuthenticator>> CreateAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            CancellationToken cancellationToken);

        /// <summary>Updates generic metadata using optimistic concurrency.</summary>
        Task<VersionedRecord<UserAuthenticator>> UpdateAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            long expectedVersion,
            CancellationToken cancellationToken);
    }
}
