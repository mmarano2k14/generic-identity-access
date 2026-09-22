namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents the public RSA JWK exposed by the OIDC provider.</summary>
    /// <param name="KeyType">The JWK key type.</param>
    /// <param name="KeyId">The signing key identifier.</param>
    /// <param name="Use">The JWK key use.</param>
    /// <param name="Algorithm">The signing algorithm.</param>
    /// <param name="Modulus">The RSA modulus encoded as base64url.</param>
    /// <param name="Exponent">The RSA exponent encoded as base64url.</param>
    public sealed record OidcJsonWebKey(
        string KeyType,
        string KeyId,
        string Use,
        string Algorithm,
        string Modulus,
        string Exponent);
}
