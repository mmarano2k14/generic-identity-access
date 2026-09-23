using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa
{
    public sealed class UserAuthenticatorModelTests
    {
        [Fact]
        public void Active_authenticator_requires_confirmation()
        {
            var now = DateTimeOffset.UtcNow;
            Assert.Throws<ArgumentException>(() =>
                new UserAuthenticator(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new AuthenticationFactorProviderKey("totp"),
                    "Phone authenticator",
                    UserAuthenticatorStatus.Active,
                    now,
                    null,
                    null,
                    null));
        }

        [Fact]
        public void Generic_authenticator_does_not_store_provider_secret_material()
        {
            var properties = typeof(UserAuthenticator).GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.DoesNotContain(properties, name => name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(properties, name => name.Contains("Credential", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(properties, name => name.Contains("Payload", StringComparison.OrdinalIgnoreCase));
        }
    }
}
