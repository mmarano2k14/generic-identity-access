using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Verifies RS256 token issuance and process-pinned multi-key JWKS publication.</summary>
    public sealed class RsaOidcTokenIssuerTests
    {
        /// <summary>Verifies newly issued JWTs use the active private key while retained public keys remain published.</summary>
        [Fact]
        public void Active_key_signs_tokens_and_retained_public_key_remains_in_jwks()
        {
            var directory = CreateDirectory();
            var activePath = Path.Combine(directory, "active-private.pem");
            var retainedPath = Path.Combine(directory, "retained-public.pem");

            try
            {
                using var activeRsa = CreateRsa();
                using var retainedRsa = CreateRsa();

                File.WriteAllText(
                    activePath,
                    activeRsa.ExportPkcs8PrivateKeyPem());

                File.WriteAllText(
                    retainedPath,
                    retainedRsa.ExportSubjectPublicKeyInfoPem());

                using var issuer = new RsaOidcTokenIssuer(
                    Options(),
                    "key-2",
                    [
                        new RsaOidcSigningKeyOptions(
                            "key-1",
                            retainedPath),
                        new RsaOidcSigningKeyOptions(
                            "key-2",
                            activePath)
                    ]);

                var now = DateTimeOffset.UtcNow;

                var tokens = issuer.Issue(
                    new OidcTokenIssueRequest(
                        new SubjectReference(
                            Guid.Parse("37111111-1111-1111-1111-111111111111"),
                            Guid.Parse("37aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                        Guid.Parse("37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        "web-client",
                        new ApplicationKey("app-a"),
                        "openid",
                        "nonce-12345678",
                        now.AddMinutes(-1),
                        now));

                VerifySignature(
                    tokens.AccessToken,
                    activeRsa,
                    "key-2");

                VerifySignature(
                    tokens.IdToken,
                    activeRsa,
                    "key-2");

                Assert.False(
                    VerifySignatureOnly(
                        tokens.AccessToken,
                        retainedRsa));

                using var idPayload = Payload(tokens.IdToken);

                Assert.Equal(
                    "https://identity.example.test",
                    idPayload.RootElement.GetProperty("iss").GetString());

                Assert.Equal(
                    "web-client",
                    idPayload.RootElement.GetProperty("aud").GetString());

                Assert.Equal(
                    "nonce-12345678",
                    idPayload.RootElement.GetProperty("nonce").GetString());

                Assert.True(
                    idPayload.RootElement.TryGetProperty(
                        "at_hash",
                        out _));

                Assert.Equal("key-2", issuer.SigningKey.KeyId);
                Assert.Equal(2, issuer.SigningKeys.Count);
                Assert.Equal("key-2", issuer.SigningKeys[0].KeyId);
                Assert.Contains(
                    "key-1",
                    issuer.SigningKeys.Select(key => key.KeyId));

                Assert.All(
                    issuer.SigningKeys,
                    key =>
                    {
                        Assert.Equal("RSA", key.KeyType);
                        Assert.Equal("RS256", key.Algorithm);
                        Assert.Equal("sig", key.Use);
                        Assert.False(string.IsNullOrWhiteSpace(key.Modulus));
                        Assert.False(string.IsNullOrWhiteSpace(key.Exponent));
                    });
            }
            finally
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }

        /// <summary>Verifies the active key must contain private RSA material.</summary>
        [Fact]
        public void Active_key_cannot_be_public_only()
        {
            var directory = CreateDirectory();
            var keyPath = Path.Combine(directory, "public.pem");

            try
            {
                using var rsa = CreateRsa();

                File.WriteAllText(
                    keyPath,
                    rsa.ExportSubjectPublicKeyInfoPem());

                var error = Assert.Throws<InvalidOperationException>(
                    () => new RsaOidcTokenIssuer(
                        Options(),
                        "key-1",
                        [
                            new RsaOidcSigningKeyOptions(
                                "key-1",
                                keyPath)
                        ]));

                Assert.Contains(
                    "private RSA key material",
                    error.Message,
                    StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }

        /// <summary>Verifies key identifiers are unique within the published key ring.</summary>
        [Fact]
        public void Duplicate_key_ids_are_rejected()
        {
            var directory = CreateDirectory();
            var firstPath = Path.Combine(directory, "first.pem");
            var secondPath = Path.Combine(directory, "second.pem");

            try
            {
                using var first = CreateRsa();
                using var second = CreateRsa();

                File.WriteAllText(
                    firstPath,
                    first.ExportPkcs8PrivateKeyPem());

                File.WriteAllText(
                    secondPath,
                    second.ExportSubjectPublicKeyInfoPem());

                var error = Assert.Throws<InvalidOperationException>(
                    () => new RsaOidcTokenIssuer(
                        Options(),
                        "same-key",
                        [
                            new RsaOidcSigningKeyOptions(
                                "same-key",
                                firstPath),
                            new RsaOidcSigningKeyOptions(
                                "same-key",
                                secondPath)
                        ]));

                Assert.Contains(
                    "must be unique",
                    error.Message,
                    StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }

        /// <summary>Verifies the configured active key id must identify one member of the key ring.</summary>
        [Fact]
        public void Unknown_active_key_id_is_rejected()
        {
            var directory = CreateDirectory();
            var keyPath = Path.Combine(directory, "retained.pem");

            try
            {
                using var rsa = CreateRsa();

                File.WriteAllText(
                    keyPath,
                    rsa.ExportSubjectPublicKeyInfoPem());

                var error = Assert.Throws<InvalidOperationException>(
                    () => new RsaOidcTokenIssuer(
                        Options(),
                        "missing-key",
                        [
                            new RsaOidcSigningKeyOptions(
                                "retained-key",
                                keyPath)
                        ]));

                Assert.Contains(
                    "does not identify",
                    error.Message,
                    StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }

        private static OidcOptions Options() =>
            new()
            {
                Issuer = "https://identity.example.test",
                AccessTokenAudience = "identity-api"
            };

        private static RSA CreateRsa()
        {
            var rsa = RSA.Create();
            rsa.KeySize = 2048;
            return rsa;
        }

        private static string CreateDirectory()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "identity-access-oidc-key-ring-" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void VerifySignature(
            string token,
            RSA rsa,
            string expectedKeyId)
        {
            Assert.True(
                VerifySignatureOnly(
                    token,
                    rsa));

            var parts = token.Split('.');

            using var header = JsonDocument.Parse(
                Decode(parts[0]));

            Assert.Equal(
                "RS256",
                header.RootElement.GetProperty("alg").GetString());

            Assert.Equal(
                expectedKeyId,
                header.RootElement.GetProperty("kid").GetString());
        }

        private static bool VerifySignatureOnly(
            string token,
            RSA rsa)
        {
            var parts = token.Split('.');

            Assert.Equal(3, parts.Length);

            var signingInput =
                Encoding.ASCII.GetBytes(
                    $"{parts[0]}.{parts[1]}");

            var signature =
                Decode(parts[2]);

            return rsa.VerifyData(
                signingInput,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }

        private static JsonDocument Payload(
            string token)
        {
            var parts = token.Split('.');
            return JsonDocument.Parse(
                Decode(parts[1]));
        }

        private static byte[] Decode(
            string value)
        {
            var padded =
                value
                    .Replace('-', '+')
                    .Replace('_', '/');

            padded +=
                new string(
                    '=',
                    (4 - padded.Length % 4) % 4);

            return Convert.FromBase64String(
                padded);
        }
    }
}
