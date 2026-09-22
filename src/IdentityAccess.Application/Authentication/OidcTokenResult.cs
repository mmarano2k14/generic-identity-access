namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents a successful token response or a typed token-endpoint failure.</summary>
    public sealed record OidcTokenResult
    {
        /// <summary>Gets whether token issuance succeeded.</summary>
        public bool Succeeded { get; }

        /// <summary>Gets the bearer access token.</summary>
        public string? AccessToken { get; }

        /// <summary>Gets the OpenID Connect ID token when this response includes one.</summary>
        public string? IdToken { get; }

        /// <summary>Gets the newly issued opaque refresh token.</summary>
        public string? RefreshToken { get; }

        /// <summary>Gets the access-token lifetime in seconds.</summary>
        public int ExpiresIn { get; }

        /// <summary>Gets the granted canonical scope string.</summary>
        public string? Scope { get; }

        /// <summary>Gets the typed failure category.</summary>
        public OidcTokenFailureCode? FailureCode { get; }

        /// <summary>Initializes a new token result.</summary>
        public OidcTokenResult(
            bool succeeded,
            string? accessToken = null,
            string? idToken = null,
            string? refreshToken = null,
            int expiresIn = 0,
            string? scope = null,
            OidcTokenFailureCode? failureCode = null)
        {
            Succeeded = succeeded;
            AccessToken = accessToken;
            IdToken = idToken;
            RefreshToken = refreshToken;
            ExpiresIn = expiresIn;
            Scope = scope;
            FailureCode = failureCode;
        }

        /// <summary>Creates a successful authorization-code token result.</summary>
        public static OidcTokenResult AuthorizationCodeSuccess(
            OidcIssuedTokens tokens,
            string refreshToken,
            string scope)
        {
            ArgumentNullException.ThrowIfNull(tokens);
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
            ArgumentException.ThrowIfNullOrWhiteSpace(scope);

            return new OidcTokenResult(
                true,
                tokens.AccessToken,
                tokens.IdToken,
                refreshToken,
                tokens.ExpiresIn,
                scope);
        }

        /// <summary>Creates a successful refresh-token result without issuing a new ID token.</summary>
        public static OidcTokenResult RefreshSuccess(
            OidcIssuedAccessToken token,
            string refreshToken,
            string scope)
        {
            ArgumentNullException.ThrowIfNull(token);
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
            ArgumentException.ThrowIfNullOrWhiteSpace(scope);

            return new OidcTokenResult(
                true,
                token.AccessToken,
                null,
                refreshToken,
                token.ExpiresIn,
                scope);
        }

        /// <summary>Creates a failed token result.</summary>
        public static OidcTokenResult Failure(
            OidcTokenFailureCode failureCode) =>
            new(false, failureCode: failureCode);
    }
}
