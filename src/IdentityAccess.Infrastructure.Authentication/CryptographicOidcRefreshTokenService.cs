using System.Security.Cryptography;
using System.Text;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>Issues 256-bit opaque refresh tokens and computes SHA-256 persistence hashes.</summary>
    internal sealed class CryptographicOidcRefreshTokenService :
        IOidcRefreshTokenService
    {
        /// <inheritdoc />
        public IssuedOidcRefreshToken Issue()
        {
            var bytes =
                RandomNumberGenerator.GetBytes(32);

            var value =
                OidcBase64Url.Encode(bytes);

            return new IssuedOidcRefreshToken(
                value,
                SHA256.HashData(
                    Encoding.ASCII.GetBytes(value)));
        }

        /// <inheritdoc />
        public byte[] Hash(
            string refreshToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

            return SHA256.HashData(
                Encoding.ASCII.GetBytes(refreshToken));
        }
    }
}
