namespace IdentityAccess.Api.Security
{
    /// <summary>Defines trusted administration-context resolution outcomes.</summary>
    public enum AdministrationAuthenticationDecision
    {
        /// <summary>A trusted context was established from a valid local session.</summary>
        Authenticated = 1,

        /// <summary>No valid administration authentication credential was supplied.</summary>
        Unauthenticated = 2,

        /// <summary>The local authentication capability is unavailable on the host.</summary>
        Unavailable = 3
    }
}
