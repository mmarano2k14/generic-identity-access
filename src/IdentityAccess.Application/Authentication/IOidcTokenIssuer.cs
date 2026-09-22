namespace IdentityAccess.Application.Authentication
{
    /// <summary>Issues signed OpenID Connect tokens and exposes the process-pinned public signing keys.</summary>
    public interface IOidcTokenIssuer
    {
        /// <summary>Gets the active public signing key used for newly issued tokens.</summary>
        OidcJsonWebKey SigningKey { get; }

        /// <summary>Gets all public signing keys currently published through JWKS.</summary>
        IReadOnlyList<OidcJsonWebKey> SigningKeys { get; }

        /// <summary>Issues a signed access-token and ID-token pair for an authorization-code exchange.</summary>
        OidcIssuedTokens Issue(
            OidcTokenIssueRequest request);

        /// <summary>Issues a signed access token without creating a new ID token.</summary>
        OidcIssuedAccessToken IssueAccessToken(
            OidcAccessTokenIssueRequest request);
    }
}
