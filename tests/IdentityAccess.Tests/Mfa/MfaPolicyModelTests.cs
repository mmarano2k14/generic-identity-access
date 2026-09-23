using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa
{
    public sealed class MfaPolicyModelTests
    {
        [Fact]
        public void Disabled_policy_requires_no_provider()
        {
            var policy = new MfaPolicy(
                Guid.NewGuid(),
                new ApplicationKey("admin-web"),
                MfaPolicyMode.Disabled,
                Array.Empty<AuthenticationFactorProviderKey>());

            Assert.Equal(MfaPolicyMode.Disabled, policy.Mode);
            Assert.Empty(policy.AllowedProviders);
        }

        [Fact]
        public void Enabled_policy_requires_at_least_one_provider()
        {
            Assert.Throws<ArgumentException>(() =>
                new MfaPolicy(
                    Guid.NewGuid(),
                    new ApplicationKey("admin-web"),
                    MfaPolicyMode.Required,
                    Array.Empty<AuthenticationFactorProviderKey>()));
        }

        [Fact]
        public void Policy_deduplicates_and_orders_provider_keys()
        {
            var policy = new MfaPolicy(
                Guid.NewGuid(),
                new ApplicationKey("admin-web"),
                MfaPolicyMode.Optional,
                [
                    new AuthenticationFactorProviderKey("webauthn"),
                    new AuthenticationFactorProviderKey("totp"),
                    new AuthenticationFactorProviderKey("webauthn")
                ]);

            Assert.Equal(new[] { "totp", "webauthn" }, policy.AllowedProviders.Select(item => item.Value));
        }
    }
}
