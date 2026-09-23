using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Mfa.Recovery;

namespace IdentityAccess.Tests.Mfa.Recovery
{
    public sealed class RecoveryAuthenticationFactorProviderTests
    {
        [Fact]
        public void Descriptor_declares_enrollment_verification_and_recovery()
        {
            var provider = new RecoveryAuthenticationFactorProvider();

            Assert.Equal("recovery", provider.Descriptor.Key.Value);
            Assert.Equal(
                AuthenticationFactorProviderCapabilities.Enrollment |
                AuthenticationFactorProviderCapabilities.Verification |
                AuthenticationFactorProviderCapabilities.Recovery,
                provider.Descriptor.Capabilities);
        }
    }
}
