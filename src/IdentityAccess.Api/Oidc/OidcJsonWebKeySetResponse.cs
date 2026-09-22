using System.Text.Json.Serialization;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>Represents the provider JSON Web Key Set.</summary>
    /// <param name="Keys">The published public signing keys.</param>
    public sealed record OidcJsonWebKeySetResponse(
        [property: JsonPropertyName("keys")]
        IReadOnlyList<OidcJsonWebKeyResponse> Keys);
}
