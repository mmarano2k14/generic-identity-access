using System.Security.Cryptography;
using System.Text;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>
    /// Issues opaque 256-bit session tokens and derives the SHA-256 token hash persisted by the
    /// authentication session store.
    /// </summary>
    internal sealed class CryptographicSessionTokenService : ISessionTokenService
    {
        /// <inheritdoc />
        public IssuedSessionToken Issue()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Base64Url(bytes);

            return new IssuedSessionToken(
                token,
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        /// <inheritdoc />
        public byte[] Hash(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            if (token.Length > 512)
            {
                throw new ArgumentException(
                    "The session token is invalid.",
                    nameof(token));
            }

            return SHA256.HashData(Encoding.UTF8.GetBytes(token));
        }

        private static string Base64Url(byte[] value) =>
            Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }
}
