namespace IdentityAccess.Authorization
{
    /// <summary>
    /// Defines stable technical failure categories produced by identity authorization orchestration.
    /// </summary>
    public enum IdentityAuthorizationFailureCode
    {
        /// <summary>The database route required for authorization could not be resolved.</summary>
        RouteResolutionFailed = 1,

        /// <summary>The current assigned-capability projection could not be read.</summary>
        GrantProjectionFailed = 2,

        /// <summary>A projected grant did not belong to the requested subject, authorization boundary, or application context.</summary>
        GrantProvenanceMismatch = 3,

        /// <summary>Projected grants could not be materialized into the external RBAC TRN representation.</summary>
        GrantMaterializationFailed = 4,

        /// <summary>The configured RBAC adapter threw before returning a typed result.</summary>
        RbacAdapterInvocationFailed = 5,

        /// <summary>The external RBAC boundary returned a typed technical failure.</summary>
        RbacTechnicalFailure = 6,

        /// <summary>The external RBAC boundary returned an unknown decision value.</summary>
        UnknownRbacDecision = 7
    }
}
