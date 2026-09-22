using IdentityAccess.Infrastructure.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Verifies opaque refresh-token generation and SHA-256 persistence hashing.</summary>
    public sealed class CryptographicOidcRefreshTokenServiceTests
    {
        /// <summary>Verifies refresh tokens contain 256 bits of random input and only expose a hash for persistence.</summary>
        [Fact]
        public void Issue_returns_base64url_token_and_sha256_hash()
        {
            var service =
                new CryptographicOidcRefreshTokenService();

            var first = service.Issue();
            var second = service.Issue();

            Assert.Equal(43, first.Value.Length);
            Assert.All(
                first.Value,
                character =>
                    Assert.True(
                        char.IsAsciiLetterOrDigit(character) ||
                        character is '-' or '_'));
            Assert.Equal(32, first.Hash.Length);
            Assert.Equal(
                first.Hash,
                service.Hash(first.Value));
            Assert.NotEqual(first.Value, second.Value);
        }
    }
}
