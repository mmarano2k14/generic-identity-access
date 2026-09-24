namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines stable OpenID Connect authorization-endpoint failure categories.</summary>
    public enum OidcAuthorizationFailureCode
    {
        /// <summary>The request is malformed.</summary>
        InvalidRequest = 1,

        /// <summary>The requested client is unknown or not OIDC-enabled.</summary>
        UnknownClient = 2,

        /// <summary>The redirect URI is not exactly registered.</summary>
        RedirectUriRejected = 3,

        /// <summary>The requested response type is unsupported.</summary>
        UnsupportedResponseType = 4,

        /// <summary>The requested scope is unsupported.</summary>
        InvalidScope = 5,

        /// <summary>PKCE is missing or does not use S256.</summary>
        PkceRequired = 6,

        /// <summary>The PKCE challenge is malformed.</summary>
        InvalidPkce = 7,

        /// <summary>An authenticated local session is required.</summary>
        LoginRequired = 8,

        /// <summary>The configured identity directory is unavailable.</summary>
        DirectoryUnavailable = 9,

        /// <summary>The current local session requires a recent additional authentication factor.</summary>
        MfaRequired = 10
    }
}
