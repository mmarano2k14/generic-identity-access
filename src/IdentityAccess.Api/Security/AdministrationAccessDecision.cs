namespace IdentityAccess.Api.Security
{
    /// <summary>Defines administration access decisions.</summary>
    public enum AdministrationAccessDecision
    {
        /// <summary>The administration request is authorized.</summary>
        Allowed = 1,

        /// <summary>The caller has not established a trusted authenticated administration context.</summary>
        Unauthenticated = 2,

        /// <summary>The authenticated administration context is explicitly denied.</summary>
        Denied = 3,

        /// <summary>The required authorization capability cannot currently evaluate the request.</summary>
        Unavailable = 4
    }
}
