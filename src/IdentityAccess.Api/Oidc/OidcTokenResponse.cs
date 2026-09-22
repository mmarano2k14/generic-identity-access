using System.Text.Json.Serialization;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>Represents a successful OAuth token response.</summary>
    /// <param name="AccessToken">The bearer access token.</param>
    /// <param name="TokenType">The OAuth token type.</param>
    /// <param name="ExpiresIn">The access-token lifetime in seconds.</param>
    /// <param name="IdToken">The OpenID Connect ID token when issued by this grant.</param>
    /// <param name="RefreshToken">The newly issued opaque refresh token.</param>
    /// <param name="Scope">The granted scope string.</param>
    public sealed record OidcTokenResponse(
        [property: JsonPropertyName("access_token")]
        string AccessToken,

        [property: JsonPropertyName("token_type")]
        string TokenType,

        [property: JsonPropertyName("expires_in")]
        int ExpiresIn,

        [property: JsonPropertyName("id_token")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? IdToken,

        [property: JsonPropertyName("refresh_token")]
        string RefreshToken,

        [property: JsonPropertyName("scope")]
        string Scope);
}
