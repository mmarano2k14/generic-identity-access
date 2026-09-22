namespace IdentityAccess.Rbac
{
    /// <summary>
    /// Defines stable technical failure categories returned by an RBAC adapter.
    /// </summary>
    public enum RbacAuthorizationFailureCode
    {
        /// <summary>The configured external RBAC binaries are unavailable.</summary>
        ExternalBinariesMissing = 1,

        /// <summary>The external RBAC engine could not be invoked successfully.</summary>
        ExternalInvocationFailed = 2,

        /// <summary>The external RBAC binary distribution could not be loaded.</summary>
        ExternalLoadFailed = 3,

        /// <summary>The external RBAC binary distribution does not satisfy the required adapter contract.</summary>
        ExternalContractMismatch = 4
    }
}
