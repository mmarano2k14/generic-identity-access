namespace IdentityAccess.Api.Security
{
    /// <summary>Defines stable failures for administration authentication-context resolution.</summary>
    public enum AdministrationAuthenticationFailureCode
    {
        /// <summary>The request did not contain the complete local-session credential set.</summary>
        CredentialsMissing = 1,

        /// <summary>The supplied local-session credential set was malformed.</summary>
        CredentialsMalformed = 2,

        /// <summary>The supplied local session was invalid, expired, revoked, or no longer active.</summary>
        SessionInvalid = 3,

        /// <summary>Local authentication is unavailable on the current host.</summary>
        AuthenticationUnavailable = 4
    }
}
