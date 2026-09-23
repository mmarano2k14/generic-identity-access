using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.Authentication;

namespace IdentityAccess.Tests.Mfa
{
    public sealed class AuthenticationFactorProviderRegistryTests
    {
        [Fact]
        public void Registry_lists_providers_in_stable_key_order()
        {
            var registry = new AuthenticationFactorProviderRegistry(
            [
                new TestAuthenticationFactorProvider(
                    "webauthn",
                    AuthenticationFactorProviderCapabilities.Enrollment |
                    AuthenticationFactorProviderCapabilities.Verification),
                new TestAuthenticationFactorProvider(
                    "totp",
                    AuthenticationFactorProviderCapabilities.Enrollment |
                    AuthenticationFactorProviderCapabilities.Verification)
            ]);

            Assert.Equal(new[] { "totp", "webauthn" }, registry.List().Select(item => item.Key.Value));
            Assert.NotNull(registry.Find(new AuthenticationFactorProviderKey("totp")));
            Assert.Null(registry.Find(new AuthenticationFactorProviderKey("recovery")));
        }

        [Fact]
        public void Registry_rejects_duplicate_provider_keys()
        {
            var providers = new IAuthenticationFactorProvider[]
            {
                new TestAuthenticationFactorProvider(
                    "totp",
                    AuthenticationFactorProviderCapabilities.Enrollment),
                new TestAuthenticationFactorProvider(
                    "totp",
                    AuthenticationFactorProviderCapabilities.Verification)
            };

            Assert.Throws<InvalidOperationException>(() =>
                new AuthenticationFactorProviderRegistry(providers));
        }
    }
}
