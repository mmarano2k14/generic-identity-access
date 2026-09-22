using System.Security.Cryptography;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Verifies strict RS256 access-token validation and registered-client binding.</summary>
    public sealed class RsaOidcAccessTokenValidatorTests
    {
        /// <summary>Verifies a valid provider token becomes a trusted server-bound token context.</summary>
        [Fact]
        public void Valid_access_token_is_accepted_and_bound_to_registered_client()
        {
            var directory = CreateDirectory();
            var keyPath = Path.Combine(directory, "active.pem");

            try
            {
                using var rsa = CreateRsa();
                File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());

                using var issuer = CreateIssuer(Options(), "key-1", keyPath);
                var registry = Registry();
                using var validator = new RsaOidcAccessTokenValidator(
                    Options(),
                    registry,
                    issuer,
                    TimeProvider.System);

                var now = DateTimeOffset.UtcNow;
                var subject = new SubjectReference(
                    Guid.Parse("40111111-1111-1111-1111-111111111111"),
                    Guid.Parse("40aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
                var sessionId = Guid.Parse("40bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

                var issued = issuer.IssueAccessToken(
                    new OidcAccessTokenIssueRequest(
                        subject,
                        sessionId,
                        "web-client",
                        new ApplicationKey("app-a"),
                        "openid",
                        now));

                var result = validator.Validate(issued.AccessToken);

                Assert.True(result.Valid);
                Assert.NotNull(result.Token);
                Assert.Null(result.FailureCode);
                Assert.Equal(subject, result.Token!.Subject);
                Assert.Equal(sessionId, result.Token.SessionId);
                Assert.Equal("web-client", result.Token.ClientId);
                Assert.Equal(new ApplicationKey("app-a"), result.Token.Application);
                Assert.Equal("app-a-primary", result.Token.AuthenticationContextKey);
                Assert.Equal("openid", result.Token.Scope);
                Assert.True(result.Token.ExpiresAt > result.Token.IssuedAt);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>Verifies a retained public key validates tokens issued before active-key rotation.</summary>
        [Fact]
        public void Retained_public_key_validates_pre_rotation_access_token()
        {
            var directory = CreateDirectory();
            var oldPrivatePath = Path.Combine(directory, "old-private.pem");
            var oldPublicPath = Path.Combine(directory, "old-public.pem");
            var newPrivatePath = Path.Combine(directory, "new-private.pem");

            try
            {
                using var oldRsa = CreateRsa();
                using var newRsa = CreateRsa();

                File.WriteAllText(oldPrivatePath, oldRsa.ExportPkcs8PrivateKeyPem());
                File.WriteAllText(oldPublicPath, oldRsa.ExportSubjectPublicKeyInfoPem());
                File.WriteAllText(newPrivatePath, newRsa.ExportPkcs8PrivateKeyPem());

                using var oldIssuer = CreateIssuer(Options(), "key-1", oldPrivatePath);
                using var rotatedIssuer = new RsaOidcTokenIssuer(
                    Options(),
                    "key-2",
                    [
                        new RsaOidcSigningKeyOptions("key-2", newPrivatePath),
                        new RsaOidcSigningKeyOptions("key-1", oldPublicPath)
                    ]);
                using var validator = new RsaOidcAccessTokenValidator(
                    Options(),
                    Registry(),
                    rotatedIssuer,
                    TimeProvider.System);

                var issued = Issue(oldIssuer, DateTimeOffset.UtcNow, new ApplicationKey("app-a"));

                var result = validator.Validate(issued.AccessToken);

                Assert.True(result.Valid);
                Assert.NotNull(result.Token);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>Verifies an unknown kid is rejected before any fallback key is attempted.</summary>
        [Fact]
        public void Unknown_signing_key_is_rejected()
        {
            var directory = CreateDirectory();
            var oldPath = Path.Combine(directory, "old.pem");
            var currentPath = Path.Combine(directory, "current.pem");

            try
            {
                using var oldRsa = CreateRsa();
                using var currentRsa = CreateRsa();
                File.WriteAllText(oldPath, oldRsa.ExportPkcs8PrivateKeyPem());
                File.WriteAllText(currentPath, currentRsa.ExportPkcs8PrivateKeyPem());

                using var oldIssuer = CreateIssuer(Options(), "old-key", oldPath);
                using var currentIssuer = CreateIssuer(Options(), "current-key", currentPath);
                using var validator = new RsaOidcAccessTokenValidator(
                    Options(),
                    Registry(),
                    currentIssuer,
                    TimeProvider.System);

                var result = validator.Validate(
                    Issue(oldIssuer, DateTimeOffset.UtcNow, new ApplicationKey("app-a")).AccessToken);

                Assert.False(result.Valid);
                Assert.Equal(
                    OidcAccessTokenValidationFailureCode.UnknownSigningKey,
                    result.FailureCode);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>Verifies signature tampering is rejected.</summary>
        [Fact]
        public void Tampered_access_token_is_rejected()
        {
            var directory = CreateDirectory();
            var keyPath = Path.Combine(directory, "active.pem");

            try
            {
                using var rsa = CreateRsa();
                File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());

                using var issuer = CreateIssuer(Options(), "key-1", keyPath);
                using var validator = new RsaOidcAccessTokenValidator(
                    Options(),
                    Registry(),
                    issuer,
                    TimeProvider.System);

                var token = Issue(issuer, DateTimeOffset.UtcNow, new ApplicationKey("app-a")).AccessToken;
                var parts = token.Split('.');
                parts[1] = MutateBase64Url(parts[1]);

                var result = validator.Validate(string.Join('.', parts));

                Assert.False(result.Valid);
                Assert.Equal(
                    OidcAccessTokenValidationFailureCode.SignatureInvalid,
                    result.FailureCode);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>Verifies issuer, audience, expiry, and client/application binding are independently enforced.</summary>
        [Fact]
        public void Provider_and_client_bindings_are_enforced()
        {
            var directory = CreateDirectory();
            var keyPath = Path.Combine(directory, "active.pem");

            try
            {
                using var rsa = CreateRsa();
                File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());

                using var validationIssuer = CreateIssuer(Options(), "key-1", keyPath);
                using var validator = new RsaOidcAccessTokenValidator(
                    Options(),
                    Registry(),
                    validationIssuer,
                    TimeProvider.System);

                using var wrongIssuer = CreateIssuer(
                    new OidcOptions
                    {
                        Issuer = "https://other.example.test",
                        AccessTokenAudience = "identity-api"
                    },
                    "key-1",
                    keyPath);

                AssertFailure(
                    validator,
                    Issue(wrongIssuer, DateTimeOffset.UtcNow, new ApplicationKey("app-a")).AccessToken,
                    OidcAccessTokenValidationFailureCode.IssuerMismatch);

                using var wrongAudienceIssuer = CreateIssuer(
                    new OidcOptions
                    {
                        Issuer = "https://identity.example.test",
                        AccessTokenAudience = "different-api"
                    },
                    "key-1",
                    keyPath);

                AssertFailure(
                    validator,
                    Issue(wrongAudienceIssuer, DateTimeOffset.UtcNow, new ApplicationKey("app-a")).AccessToken,
                    OidcAccessTokenValidationFailureCode.AudienceMismatch);

                AssertFailure(
                    validator,
                    Issue(validationIssuer, DateTimeOffset.UtcNow.AddHours(-1), new ApplicationKey("app-a")).AccessToken,
                    OidcAccessTokenValidationFailureCode.Expired);

                AssertFailure(
                    validator,
                    Issue(validationIssuer, DateTimeOffset.UtcNow, new ApplicationKey("app-b")).AccessToken,
                    OidcAccessTokenValidationFailureCode.ClientBindingInvalid);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static void AssertFailure(
            RsaOidcAccessTokenValidator validator,
            string accessToken,
            OidcAccessTokenValidationFailureCode expectedFailure)
        {
            var result = validator.Validate(accessToken);
            Assert.False(result.Valid);
            Assert.Null(result.Token);
            Assert.Equal(expectedFailure, result.FailureCode);
        }

        private static OidcIssuedAccessToken Issue(
            RsaOidcTokenIssuer issuer,
            DateTimeOffset issuedAt,
            ApplicationKey application) =>
            issuer.IssueAccessToken(
                new OidcAccessTokenIssueRequest(
                    new SubjectReference(
                        Guid.Parse("40111111-1111-1111-1111-111111111111"),
                        Guid.Parse("40aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                    Guid.Parse("40bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "web-client",
                    application,
                    "openid",
                    issuedAt));

        private static RsaOidcTokenIssuer CreateIssuer(
            OidcOptions options,
            string keyId,
            string keyPath) =>
            new(
                options,
                keyId,
                [
                    new RsaOidcSigningKeyOptions(
                        keyId,
                        keyPath)
                ]);

        private static OidcOptions Options() =>
            new()
            {
                Issuer = "https://identity.example.test",
                AccessTokenAudience = "identity-api"
            };

        private static ConfiguredAuthenticationClientRegistry Registry() =>
            new(
                [
                    new AuthenticationClientRegistration(
                        "web-client",
                        new ApplicationKey("app-a"),
                        "app-a-primary",
                        ["https://client.example.test/callback"],
                        oidcEnabled: true,
                        allowedOidcScopes: ["openid"])
                ]);

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
                "identity-access-oidc-access-token-" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string MutateBase64Url(string value)
        {
            var characters = value.ToCharArray();
            var index = characters.Length / 2;
            characters[index] = characters[index] == 'A' ? 'B' : 'A';
            return new string(characters);
        }
    }
}
