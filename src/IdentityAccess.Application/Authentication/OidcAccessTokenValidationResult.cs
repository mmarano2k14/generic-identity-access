namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents the result of validating one OIDC access token.</summary>
    public readonly record struct OidcAccessTokenValidationResult(
        bool Valid,
        ValidatedOidcAccessToken? Token = null,
        OidcAccessTokenValidationFailureCode? FailureCode = null)
    {
        /// <summary>Creates a successful access-token validation result.</summary>
        public static OidcAccessTokenValidationResult Success(
            ValidatedOidcAccessToken token) =>
            new(
                true,
                token);

        /// <summary>Creates an invalid access-token validation result.</summary>
        public static OidcAccessTokenValidationResult Invalid(
            OidcAccessTokenValidationFailureCode failureCode) =>
            new(
                false,
                FailureCode: failureCode);
    }
}
