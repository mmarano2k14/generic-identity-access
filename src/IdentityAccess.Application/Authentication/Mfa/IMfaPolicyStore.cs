using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Persists generic MFA policy independently from provider-specific credential material.</summary>
    public interface IMfaPolicyStore
    {
        /// <summary>Gets the MFA policy for one application.</summary>
        Task<VersionedRecord<MfaPolicy>?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken);

        /// <summary>Creates the initial MFA policy.</summary>
        Task<VersionedRecord<MfaPolicy>> CreateAsync(
            ResolvedDatabaseRoute route,
            MfaPolicy policy,
            CancellationToken cancellationToken);

        /// <summary>Updates an MFA policy using optimistic concurrency.</summary>
        Task<VersionedRecord<MfaPolicy>> UpdateAsync(
            ResolvedDatabaseRoute route,
            MfaPolicy policy,
            long expectedVersion,
            CancellationToken cancellationToken);
    }
}
