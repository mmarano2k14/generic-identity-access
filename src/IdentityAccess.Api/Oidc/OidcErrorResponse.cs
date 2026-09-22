using System.Text.Json.Serialization;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>Represents an OAuth/OIDC protocol error response.</summary>
    /// <param name="Error">The OAuth/OIDC protocol error code.</param>
    public sealed record OidcErrorResponse(
        [property: JsonPropertyName("error")]
        string Error);
}
