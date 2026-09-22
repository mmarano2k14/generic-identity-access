using IdentityAccess.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Verifies trusted OIDC signing-key configuration parsing and backward compatibility.</summary>
    public sealed class RsaOidcSigningKeyConfigurationLoaderTests
    {
        /// <summary>Verifies a multi-key configuration resolves an explicit active key and relative PEM paths.</summary>
        [Fact]
        public void Key_ring_configuration_loads_active_key_and_resolves_paths()
        {
            var root = Path.GetFullPath(
                Path.Combine(
                    Path.GetTempPath(),
                    "identity-access-key-ring-config"));

            var section = Configuration(
                new Dictionary<string, string?>
                {
                    ["Oidc:ActiveSigningKeyId"] = "key-2",
                    ["Oidc:SigningKeys:0:KeyId"] = "key-1",
                    ["Oidc:SigningKeys:0:PemPath"] = "secrets/key-1-public.pem",
                    ["Oidc:SigningKeys:1:KeyId"] = "key-2",
                    ["Oidc:SigningKeys:1:PemPath"] = "secrets/key-2-private.pem"
                });

            var result =
                RsaOidcSigningKeyConfigurationLoader.Load(
                    section,
                    root);

            Assert.Equal(
                "key-2",
                result.ActiveSigningKeyId);

            Assert.Equal(
                2,
                result.SigningKeys.Count);

            Assert.Equal(
                Path.GetFullPath(
                    "secrets/key-1-public.pem",
                    root),
                result.SigningKeys[0].KeyPemPath);

            Assert.Equal(
                Path.GetFullPath(
                    "secrets/key-2-private.pem",
                    root),
                result.SigningKeys[1].KeyPemPath);
        }

        /// <summary>Verifies the previous single-key configuration remains a supported compatibility form.</summary>
        [Fact]
        public void Legacy_single_key_configuration_remains_supported()
        {
            var root = Path.GetTempPath();

            var section = Configuration(
                new Dictionary<string, string?>
                {
                    ["Oidc:SigningKeyId"] = "legacy-key",
                    ["Oidc:SigningKeyPemPath"] = "legacy.pem"
                });

            var result =
                RsaOidcSigningKeyConfigurationLoader.Load(
                    section,
                    root);

            Assert.Equal(
                "legacy-key",
                result.ActiveSigningKeyId);

            var key =
                Assert.Single(
                    result.SigningKeys);

            Assert.Equal(
                "legacy-key",
                key.KeyId);
        }

        /// <summary>Verifies legacy and multi-key configuration forms cannot be combined.</summary>
        [Fact]
        public void Mixed_legacy_and_key_ring_configuration_is_rejected()
        {
            var section = Configuration(
                new Dictionary<string, string?>
                {
                    ["Oidc:ActiveSigningKeyId"] = "key-2",
                    ["Oidc:SigningKeys:0:KeyId"] = "key-2",
                    ["Oidc:SigningKeys:0:PemPath"] = "key-2.pem",
                    ["Oidc:SigningKeyId"] = "legacy-key",
                    ["Oidc:SigningKeyPemPath"] = "legacy.pem"
                });

            var error = Assert.Throws<InvalidOperationException>(
                () => RsaOidcSigningKeyConfigurationLoader.Load(
                    section,
                    Path.GetTempPath()));

            Assert.Contains(
                "cannot be combined",
                error.Message,
                StringComparison.Ordinal);
        }

        private static IConfigurationSection Configuration(
            IReadOnlyDictionary<string, string?> values) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build()
                .GetSection("Oidc");
    }
}
