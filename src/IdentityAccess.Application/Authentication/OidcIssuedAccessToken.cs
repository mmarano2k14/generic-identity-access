namespace IdentityAccess.Application.Authentication
{
    /// <summary>Contains one signed bearer access token.</summary>
    /// <param name="AccessToken">The signed bearer access token.</param>
    /// <param name="ExpiresIn">The access-token lifetime in seconds.</param>
    public sealed record OidcIssuedAccessToken(
        string AccessToken,
        int ExpiresIn);
}
