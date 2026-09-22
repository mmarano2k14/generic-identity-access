namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Revalidates the current local session/user state referenced by a cryptographically validated
    /// OIDC access token.
    /// </summary>
    public interface IOidcAccessTokenSessionValidator
    {
        /// <summary>
        /// Returns <see langword="true"/> only while the referenced local session and current user
        /// remain eligible for authenticated API access.
        /// </summary>
        Task<bool> ValidateAsync(
            ValidatedOidcAccessToken token,
            CancellationToken cancellationToken);
    }
}
