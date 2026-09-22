using System.Security.Cryptography;
using System.Text;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Provides deterministic authorization-code issuance with real SHA-256 PKCE math.</summary>
    internal sealed class OidcTestCodeService :
        IOidcCodeService
    {
        private const string FixedCode =
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";

        /// <summary>Gets the deterministic raw authorization code.</summary>
        public string Code => FixedCode;

        /// <inheritdoc />
        public IssuedOidcAuthorizationCode Issue() =>
            new(
                FixedCode,
                Hash(FixedCode));

        /// <inheritdoc />
        public byte[] Hash(
            string code) =>
            SHA256.HashData(
                Encoding.ASCII.GetBytes(code));

        /// <inheritdoc />
        public string ComputeS256Challenge(
            string codeVerifier)
        {
            var hash =
                SHA256.HashData(
                    Encoding.ASCII.GetBytes(
                        codeVerifier));

            return Convert
                .ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
