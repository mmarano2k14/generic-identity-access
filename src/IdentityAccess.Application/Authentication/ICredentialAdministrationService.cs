using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines the contract for credential administration service.</summary>
    public interface ICredentialAdministrationService
    {
        /// <summary>Gets password credential metadata for the requested user.</summary>
        Task<CredentialMetadata?> GetAsync(Guid identityScopeId, ApplicationKey application, Guid userId,
            CancellationToken cancellationToken);

        /// <summary>Creates a password credential for the requested user.</summary>
        Task<CredentialMetadata> CreateAsync(Guid identityScopeId, ApplicationKey application, Guid userId,
            string loginIdentifier, string password, CancellationToken cancellationToken);

        /// <summary>Changes the password credential using optimistic concurrency.</summary>
        Task<CredentialMetadata> ChangePasswordAsync(Guid identityScopeId, ApplicationKey application, Guid userId,
            string loginIdentifier, string password, long expectedVersion, CancellationToken cancellationToken);
    }
}
