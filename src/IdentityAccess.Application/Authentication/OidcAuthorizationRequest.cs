namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents one raw OAuth 2.0 / OpenID Connect authorization request.</summary>
    /// <param name="ClientId">The registered public client identifier.</param>
    /// <param name="RedirectUri">The exact requested redirect URI.</param>
    /// <param name="ResponseType">The requested OAuth response type.</param>
    /// <param name="Scope">The requested scope string.</param>
    /// <param name="State">The client correlation/CSRF state value.</param>
    /// <param name="Nonce">The OpenID Connect nonce.</param>
    /// <param name="CodeChallenge">The PKCE S256 code challenge.</param>
    /// <param name="CodeChallengeMethod">The PKCE challenge method.</param>
    public sealed record OidcAuthorizationRequest(
        string? ClientId,
        string? RedirectUri,
        string? ResponseType,
        string? Scope,
        string? State,
        string? Nonce,
        string? CodeChallenge,
        string? CodeChallengeMethod);
}
