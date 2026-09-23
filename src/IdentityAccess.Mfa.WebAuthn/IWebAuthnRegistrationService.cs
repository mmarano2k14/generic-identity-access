using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Creates and confirms WebAuthn public-key credential registrations.</summary>
    public interface IWebAuthnRegistrationService
    {
        /// <summary>Creates a bounded one-time registration challenge and pending authenticator.</summary>
        Task<WebAuthnRegistrationOptions> BeginRegistrationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            string displayName,
            string userName,
            string userDisplayName,
            CancellationToken cancellationToken);

        /// <summary>Validates and atomically persists one WebAuthn registration response.</summary>
        Task<WebAuthnRegistrationResult> CompleteRegistrationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            WebAuthnRegistrationResponse response,
            CancellationToken cancellationToken);
    }
}
