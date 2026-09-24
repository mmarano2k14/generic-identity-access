using IdentityAccess.Application.Authentication.Mfa;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Registers WebAuthn enrollment and assertion verification with the generic provider registry.</summary>
    internal sealed class WebAuthnAuthenticationFactorProvider : IAuthenticationFactorProvider
    {
        private static readonly AuthenticationFactorProviderDescriptor ProviderDescriptor =
            new(
                WebAuthnAuthenticationFactorProviderKey.Instance,
                "Passkey / WebAuthn",
                AuthenticationFactorProviderCapabilities.Enrollment |
                AuthenticationFactorProviderCapabilities.Verification);

        /// <inheritdoc />
        public AuthenticationFactorProviderDescriptor Descriptor => ProviderDescriptor;
    }
}
