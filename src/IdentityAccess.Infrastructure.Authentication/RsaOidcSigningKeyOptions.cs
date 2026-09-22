namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>Defines trusted server configuration for one published OIDC RSA signing key.</summary>
    internal sealed record RsaOidcSigningKeyOptions(
        string KeyId,
        string KeyPemPath);
}
