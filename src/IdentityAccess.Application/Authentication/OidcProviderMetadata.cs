namespace IdentityAccess.Application.Authentication
{
    /// <summary>Contains stable provider endpoint metadata used by OIDC discovery.</summary>
    /// <param name="Issuer">The canonical provider issuer.</param>
    /// <param name="AuthorizationEndpoint">The authorization endpoint URI.</param>
    /// <param name="TokenEndpoint">The token endpoint URI.</param>
    /// <param name="JwksUri">The JSON Web Key Set URI.</param>
    public sealed record OidcProviderMetadata(
        string Issuer,
        string AuthorizationEndpoint,
        string TokenEndpoint,
        string JwksUri);
}
