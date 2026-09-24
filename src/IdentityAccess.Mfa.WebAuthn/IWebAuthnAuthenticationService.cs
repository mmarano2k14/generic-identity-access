using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Creates and verifies WebAuthn public-key authentication assertions.</summary>
    public interface IWebAuthnAuthenticationService
    {
        /// <summary>Creates a bounded one-time authentication challenge for one known user.</summary>
        Task<WebAuthnAuthenticationOptions> BeginAuthenticationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken);

        /// <summary>Validates and atomically consumes one WebAuthn authentication assertion.</summary>
        Task<WebAuthnAuthenticationResult> CompleteAuthenticationAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid challengeId,
            WebAuthnAuthenticationResponse response,
            CancellationToken cancellationToken);
    }
}
