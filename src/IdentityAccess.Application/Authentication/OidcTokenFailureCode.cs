namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines stable OAuth token-endpoint failure categories.</summary>
    public enum OidcTokenFailureCode
    {
        /// <summary>The request is malformed.</summary>
        InvalidRequest = 1,

        /// <summary>The client is unknown or not OIDC-enabled.</summary>
        InvalidClient = 2,

        /// <summary>The authorization code, redirect URI, or PKCE verifier is invalid.</summary>
        InvalidGrant = 3,

        /// <summary>The requested grant type is unsupported.</summary>
        UnsupportedGrantType = 4,

        /// <summary>The configured identity directory is unavailable.</summary>
        DirectoryUnavailable = 5,

        /// <summary>Token issuance failed technically.</summary>
        TokenIssuanceFailed = 6
    }
}
