using System.Security.Cryptography;
using System.Text;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>
    /// Issues opaque 256-bit authorization codes, persists only SHA-256 hashes, and computes PKCE
    /// S256 challenges.
    /// </summary>
    internal sealed class CryptographicOidcCodeService :
        IOidcCodeService
    {
        /// <inheritdoc />
        public IssuedOidcAuthorizationCode Issue()
        {
            var bytes =
                RandomNumberGenerator.GetBytes(32);

            var code =
                OidcBase64Url.Encode(bytes);

            return new IssuedOidcAuthorizationCode(
                code,
                SHA256.HashData(
                    Encoding.ASCII.GetBytes(code)));
        }

        /// <inheritdoc />
        public byte[] Hash(
            string code)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            if (code.Length > 256)
            {
                throw new ArgumentException(
                    "Authorization code is invalid.",
                    nameof(code));
            }

            return SHA256.HashData(
                Encoding.ASCII.GetBytes(code));
        }

        /// <inheritdoc />
        public string ComputeS256Challenge(
            string codeVerifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);

            if (codeVerifier.Length is < 43 or > 128 ||
                codeVerifier.Any(
                    value =>
                        !(char.IsAsciiLetterOrDigit(value) ||
                            value is '-' or '.' or '_' or '~')))
            {
                throw new ArgumentException(
                    "PKCE code verifier is invalid.",
                    nameof(codeVerifier));
            }

            return OidcBase64Url.Encode(
                SHA256.HashData(
                    Encoding.ASCII.GetBytes(
                        codeVerifier)));
        }
    }
}
