using IdentityAccess.Application.Authentication.Mfa;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Registers recovery-code metadata with the generic authentication-factor provider registry.</summary>
    internal sealed class RecoveryAuthenticationFactorProvider : IAuthenticationFactorProvider
    {
        private static readonly AuthenticationFactorProviderDescriptor ProviderDescriptor =
            new(
                RecoveryAuthenticationFactorProviderKey.Instance,
                "Recovery codes",
                AuthenticationFactorProviderCapabilities.Enrollment |
                AuthenticationFactorProviderCapabilities.Verification |
                AuthenticationFactorProviderCapabilities.Recovery);

        /// <inheritdoc />
        public AuthenticationFactorProviderDescriptor Descriptor => ProviderDescriptor;
    }
}
