using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Coordinates generic MFA policy, provider metadata, and authenticator lifecycle administration.</summary>
    public interface IMfaAdministrationService
    {
        /// <summary>Lists providers registered in the current host process.</summary>
        IReadOnlyList<AuthenticationFactorProviderDescriptor> ListProviders();

        /// <summary>Gets the MFA policy for one identity scope and application.</summary>
        Task<VersionedRecord<MfaPolicy>?> GetPolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken);

        /// <summary>Creates the initial MFA policy.</summary>
        Task<VersionedRecord<MfaPolicy>> CreatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            MfaPolicyMode mode,
            IReadOnlyCollection<AuthenticationFactorProviderKey> allowedProviders,
            CancellationToken cancellationToken);

        /// <summary>Updates the MFA policy using optimistic concurrency.</summary>
        Task<VersionedRecord<MfaPolicy>> UpdatePolicyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            MfaPolicyMode mode,
            IReadOnlyCollection<AuthenticationFactorProviderKey> allowedProviders,
            long expectedVersion,
            CancellationToken cancellationToken);

        /// <summary>Lists generic authenticator metadata for one user.</summary>
        Task<IReadOnlyList<VersionedRecord<UserAuthenticator>>> ListAuthenticatorsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken);


        /// <summary>Gets the effective provider-neutral MFA state for one user.</summary>
        Task<MfaUserSecurityState> GetUserSecurityStateAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken);

        /// <summary>Revokes one authenticator metadata record without touching provider-specific material.</summary>
        Task<VersionedRecord<UserAuthenticator>?> RevokeAuthenticatorAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            long expectedVersion,
            CancellationToken cancellationToken);

        /// <summary>Revokes a lost authenticator explicitly for account recovery and revokes active user sessions.</summary>
        Task<VersionedRecord<UserAuthenticator>?> RevokeAuthenticatorForRecoveryAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            long expectedVersion,
            CancellationToken cancellationToken);
    }
}
