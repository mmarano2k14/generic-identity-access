namespace IdentityAccess.Application.Authentication
{
    /// <summary>Generates and hashes opaque OAuth refresh tokens.</summary>
    public interface IOidcRefreshTokenService
    {
        /// <summary>Issues one cryptographically random opaque refresh token.</summary>
        IssuedOidcRefreshToken Issue();

        /// <summary>Computes the SHA-256 persistence hash for a presented refresh token.</summary>
        byte[] Hash(string refreshToken);
    }
}
