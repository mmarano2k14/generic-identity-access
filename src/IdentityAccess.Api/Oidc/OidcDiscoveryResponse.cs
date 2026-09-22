using System.Text.Json.Serialization;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>Represents the OpenID Provider Configuration discovery document.</summary>
    /// <param name="Issuer">The canonical issuer.</param>
    /// <param name="AuthorizationEndpoint">The authorization endpoint.</param>
    /// <param name="TokenEndpoint">The token endpoint.</param>
    /// <param name="JwksUri">The JWKS endpoint.</param>
    /// <param name="ResponseTypesSupported">Supported OAuth response types.</param>
    /// <param name="ResponseModesSupported">Supported response modes.</param>
    /// <param name="GrantTypesSupported">Supported OAuth grant types.</param>
    /// <param name="SubjectTypesSupported">Supported OIDC subject types.</param>
    /// <param name="IdTokenSigningAlgValuesSupported">Supported ID-token signing algorithms.</param>
    /// <param name="ScopesSupported">Supported scopes.</param>
    /// <param name="TokenEndpointAuthMethodsSupported">Supported token endpoint client authentication methods.</param>
    /// <param name="CodeChallengeMethodsSupported">Supported PKCE methods.</param>
    /// <param name="ClaimsSupported">Claims emitted by the provider.</param>
    public sealed record OidcDiscoveryResponse(
        [property: JsonPropertyName("issuer")]
        string Issuer,

        [property: JsonPropertyName("authorization_endpoint")]
        string AuthorizationEndpoint,

        [property: JsonPropertyName("token_endpoint")]
        string TokenEndpoint,

        [property: JsonPropertyName("jwks_uri")]
        string JwksUri,

        [property: JsonPropertyName("response_types_supported")]
        IReadOnlyList<string> ResponseTypesSupported,

        [property: JsonPropertyName("response_modes_supported")]
        IReadOnlyList<string> ResponseModesSupported,

        [property: JsonPropertyName("grant_types_supported")]
        IReadOnlyList<string> GrantTypesSupported,

        [property: JsonPropertyName("subject_types_supported")]
        IReadOnlyList<string> SubjectTypesSupported,

        [property: JsonPropertyName("id_token_signing_alg_values_supported")]
        IReadOnlyList<string> IdTokenSigningAlgValuesSupported,

        [property: JsonPropertyName("scopes_supported")]
        IReadOnlyList<string> ScopesSupported,

        [property: JsonPropertyName("token_endpoint_auth_methods_supported")]
        IReadOnlyList<string> TokenEndpointAuthMethodsSupported,

        [property: JsonPropertyName("code_challenge_methods_supported")]
        IReadOnlyList<string> CodeChallengeMethodsSupported,

        [property: JsonPropertyName("claims_supported")]
        IReadOnlyList<string> ClaimsSupported);
}
