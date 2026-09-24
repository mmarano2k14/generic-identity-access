using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Contains one WebAuthn assertion and challenge identifier for local-session step-up.</summary>
    public sealed record WebAuthnSessionAuthenticationRequest(
        Guid ChallengeId,
        string? CredentialId,
        string? ClientDataJson,
        string? AuthenticatorData,
        string? Signature,
        string? UserHandle)
    {
        /// <summary>Creates the provider assertion after controller-level required-field validation.</summary>
        public WebAuthnAuthenticationResponse ToProviderResponse() =>
            new(
                CredentialId!,
                ClientDataJson!,
                AuthenticatorData!,
                Signature!,
                UserHandle);
    }
}
