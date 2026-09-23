using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Mfa.Totp;

namespace IdentityAccess.Tests.Mfa.Totp
{
    public sealed class TotpAuthenticationFactorProviderTests
    {
        [Fact]
        public void Descriptor_declares_enrollment_and_verification_only()
        {
            var provider = new TotpAuthenticationFactorProvider();

            Assert.Equal("totp", provider.Descriptor.Key.Value);
            Assert.Equal(
                AuthenticationFactorProviderCapabilities.Enrollment |
                AuthenticationFactorProviderCapabilities.Verification,
                provider.Descriptor.Capabilities);
        }
    }
}
