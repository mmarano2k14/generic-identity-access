using IdentityAccess.Application.Authentication.Mfa;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Registers the WebAuthn registration capability with the generic provider registry.</summary>
    internal sealed class WebAuthnAuthenticationFactorProvider : IAuthenticationFactorProvider
    {
        private static readonly AuthenticationFactorProviderDescriptor ProviderDescriptor =
            new(
                WebAuthnAuthenticationFactorProviderKey.Instance,
                "Passkey / WebAuthn",
                AuthenticationFactorProviderCapabilities.Enrollment);

        /// <inheritdoc />
        public AuthenticationFactorProviderDescriptor Descriptor => ProviderDescriptor;
    }
}
