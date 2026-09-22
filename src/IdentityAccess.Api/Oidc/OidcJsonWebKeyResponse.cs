using System.Text.Json.Serialization;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>Represents one RSA signing key in JWKS wire format.</summary>
    /// <param name="KeyType">The JWK key type.</param>
    /// <param name="KeyId">The key identifier.</param>
    /// <param name="Use">The key use.</param>
    /// <param name="Algorithm">The signing algorithm.</param>
    /// <param name="Modulus">The RSA modulus.</param>
    /// <param name="Exponent">The RSA exponent.</param>
    public sealed record OidcJsonWebKeyResponse(
        [property: JsonPropertyName("kty")]
        string KeyType,

        [property: JsonPropertyName("kid")]
        string KeyId,

        [property: JsonPropertyName("use")]
        string Use,

        [property: JsonPropertyName("alg")]
        string Algorithm,

        [property: JsonPropertyName("n")]
        string Modulus,

        [property: JsonPropertyName("e")]
        string Exponent)
    {
        /// <summary>Maps the neutral signing-key contract to JWKS wire format.</summary>
        public static OidcJsonWebKeyResponse From(
            OidcJsonWebKey key) =>
            new(
                key.KeyType,
                key.KeyId,
                key.Use,
                key.Algorithm,
                key.Modulus,
                key.Exponent);
    }
}
