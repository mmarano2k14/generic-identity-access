using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    public sealed class WebAuthnAuthenticationFactorProviderTests
    {
        [Fact]
        public void Descriptor_declares_enrollment_and_verification_capabilities()
        {
            var provider = new WebAuthnAuthenticationFactorProvider();

            Assert.Equal("webauthn", provider.Descriptor.Key.Value);
            Assert.Equal(
                AuthenticationFactorProviderCapabilities.Enrollment |
                AuthenticationFactorProviderCapabilities.Verification,
                provider.Descriptor.Capabilities);
        }
    }
}
