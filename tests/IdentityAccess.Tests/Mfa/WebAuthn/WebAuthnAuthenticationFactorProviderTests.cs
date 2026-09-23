using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    public sealed class WebAuthnAuthenticationFactorProviderTests
    {
        [Fact]
        public void Descriptor_declares_registration_capability_only_until_assertion_verification_is_added()
        {
            var provider = new WebAuthnAuthenticationFactorProvider();

            Assert.Equal("webauthn", provider.Descriptor.Key.Value);
            Assert.Equal(AuthenticationFactorProviderCapabilities.Enrollment, provider.Descriptor.Capabilities);
        }
    }
}
