namespace IdentityAccess.Application.Authentication
{
    /// <summary>Contains one signed access-token and ID-token pair.</summary>
    /// <param name="AccessToken">The signed bearer access token.</param>
    /// <param name="IdToken">The signed OpenID Connect ID token.</param>
    /// <param name="ExpiresIn">The access-token lifetime in seconds.</param>
    public sealed record OidcIssuedTokens(
        string AccessToken,
        string IdToken,
        int ExpiresIn);
}
