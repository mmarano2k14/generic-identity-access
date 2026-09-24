namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines the server-recorded assurance level of one authenticated local session.</summary>
    public enum AuthenticationAssuranceLevel
    {
        /// <summary>The session is authenticated with the local password credential only.</summary>
        PasswordOnly = 1,

        /// <summary>The password-authenticated session has completed an additional verified factor.</summary>
        MultiFactor = 2
    }
}
