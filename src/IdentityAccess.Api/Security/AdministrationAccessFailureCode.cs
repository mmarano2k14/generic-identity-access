namespace IdentityAccess.Api.Security
{
    /// <summary>Defines stable failures for administration request authorization.</summary>
    public enum AdministrationAccessFailureCode
    {
        /// <summary>The trusted administration authorization capability is unavailable.</summary>
        AuthorizationUnavailable = 1,

        /// <summary>The administration request has no trusted authenticated context.</summary>
        AuthenticationRequired = 2,

        /// <summary>The authenticated context does not match the requested identity scope or application.</summary>
        AuthenticationContextMismatch = 3,

        /// <summary>Local authentication cannot currently validate administration identity.</summary>
        AuthenticationUnavailable = 4,

        /// <summary>
        /// The current route has no tenant authorization target supported by the tenant-based grant
        /// model.
        /// </summary>
        AuthorizationTargetUnavailable = 5,

        /// <summary>The authorization orchestration or external RBAC boundary failed technically.</summary>
        AuthorizationTechnicalFailure = 6,

        /// <summary>The authenticated subject is not associated with the requested tenant boundary.</summary>
        TenantContextOutsideVisibility = 7
    }
}
