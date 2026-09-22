using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Returns deterministic signed-token placeholders for protocol orchestration tests.</summary>
    internal sealed class OidcTestTokenIssuer :
        IOidcTokenIssuer
    {
        /// <inheritdoc />
        public OidcJsonWebKey SigningKey { get; } =
            new(
                "RSA",
                "test-key",
                "sig",
                "RS256",
                "AQAB",
                "AQAB");

        /// <inheritdoc />
        public IReadOnlyList<OidcJsonWebKey> SigningKeys =>
            [SigningKey];

        /// <inheritdoc />
        public OidcIssuedTokens Issue(
            OidcTokenIssueRequest request) =>
            new(
                "access-token",
                "id-token",
                900);

        /// <inheritdoc />
        public OidcIssuedAccessToken IssueAccessToken(
            OidcAccessTokenIssueRequest request) =>
            new(
                "refresh-access-token",
                900);
    }
}
