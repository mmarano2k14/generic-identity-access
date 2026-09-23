using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa
{
    internal sealed class TestAuthenticationFactorProvider : IAuthenticationFactorProvider
    {
        public AuthenticationFactorProviderDescriptor Descriptor { get; }

        public TestAuthenticationFactorProvider(
            string key,
            AuthenticationFactorProviderCapabilities capabilities)
        {
            Descriptor = new AuthenticationFactorProviderDescriptor(
                new AuthenticationFactorProviderKey(key),
                $"Provider {key}",
                capabilities);
        }
    }
}
