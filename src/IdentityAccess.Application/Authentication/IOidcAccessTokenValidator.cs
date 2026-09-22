namespace IdentityAccess.Application.Authentication
{
    /// <summary>Validates a signed OIDC access token against trusted provider configuration.</summary>
    public interface IOidcAccessTokenValidator
    {
        /// <summary>Validates one encoded access token without retaining the raw token.</summary>
        OidcAccessTokenValidationResult Validate(string accessToken);
    }
}
