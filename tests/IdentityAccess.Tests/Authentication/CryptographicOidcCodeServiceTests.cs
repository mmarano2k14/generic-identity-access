using IdentityAccess.Infrastructure.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Verifies cryptographic authorization-code and RFC 7636 S256 behavior.</summary>
    public sealed class CryptographicOidcCodeServiceTests
    {
        /// <summary>Verifies issued codes are 256-bit opaque values and only a SHA-256 hash is returned for storage.</summary>
        [Fact]
        public void Issued_authorization_code_has_expected_entropy_and_hash_shape()
        {
            var service = new CryptographicOidcCodeService();

            var issued = service.Issue();

            Assert.Equal(43, issued.Value.Length);
            Assert.Equal(32, issued.Hash.Length);
            Assert.DoesNotContain("=", issued.Value, StringComparison.Ordinal);
        }

        /// <summary>Verifies the RFC 7636 S256 published verifier/challenge vector.</summary>
        [Fact]
        public void S256_matches_rfc7636_vector()
        {
            var service = new CryptographicOidcCodeService();

            var challenge = service.ComputeS256Challenge(
                "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk");

            Assert.Equal(
                "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
                challenge);
        }
    }
}
