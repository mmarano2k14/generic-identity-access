using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Registers TOTP metadata with the generic authentication-factor provider registry.</summary>
    internal sealed class TotpAuthenticationFactorProvider : IAuthenticationFactorProvider
    {
        private static readonly AuthenticationFactorProviderDescriptor ProviderDescriptor =
            new(
                TotpAuthenticationFactorProviderKey.Instance,
                "Authenticator app (TOTP)",
                AuthenticationFactorProviderCapabilities.Enrollment |
                AuthenticationFactorProviderCapabilities.Verification);

        /// <inheritdoc />
        public AuthenticationFactorProviderDescriptor Descriptor => ProviderDescriptor;
    }
}
