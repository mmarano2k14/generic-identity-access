using System.Security.Cryptography;
using System.Text;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Issues deterministic opaque refresh tokens for protocol orchestration tests.</summary>
    internal sealed class OidcTestRefreshTokenService :
        IOidcRefreshTokenService
    {
        private int issueCount;

        /// <summary>Gets the number of generated refresh tokens.</summary>
        public int IssueCount => issueCount;

        /// <inheritdoc />
        public IssuedOidcRefreshToken Issue()
        {
            issueCount++;

            var marker =
                (char)('A' + ((issueCount - 1) % 26));

            var value =
                new string(
                    marker,
                    43);

            return new IssuedOidcRefreshToken(
                value,
                Hash(value));
        }

        /// <inheritdoc />
        public byte[] Hash(
            string refreshToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

            return SHA256.HashData(
                Encoding.ASCII.GetBytes(
                    refreshToken));
        }
    }
}
