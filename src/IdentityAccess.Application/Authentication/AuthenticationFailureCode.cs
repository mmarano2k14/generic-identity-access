namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Defines stable failure categories returned by local authentication operations.
    /// </summary>
    public enum AuthenticationFailureCode
    {
        /// <summary>The requested authentication client is not registered.</summary>
        UnknownClient = 1,

        /// <summary>The requested login redirect URI is not registered for the client.</summary>
        RedirectUriRejected = 2,

        /// <summary>The configured identity directory cannot currently be resolved.</summary>
        DirectoryUnavailable = 3,

        /// <summary>The supplied credentials are invalid.</summary>
        InvalidCredentials = 4,

        /// <summary>The requested post-logout redirect URI is not registered for the client.</summary>
        PostLogoutRedirectUriRejected = 5,

        /// <summary>The supplied session identifier or token is invalid.</summary>
        InvalidSession = 6
    }
}
