using IdentityAccess.Rbac;

namespace IdentityAccess.Authorization
{
    /// <summary>Represents the result of identity authorization.</summary>
    public sealed record IdentityAuthorizationResult(
        IdentityAuthorizationDecision Decision,
        IdentityAuthorizationFailureCode? FailureCode = null,
        RbacAuthorizationFailureCode? RbacFailureCode = null)
    {
        /// <summary>Creates an allowed authorization result.</summary>
        public static IdentityAuthorizationResult Allow() =>
            new(IdentityAuthorizationDecision.Allowed);

        /// <summary>Creates a denied authorization result.</summary>
        public static IdentityAuthorizationResult Deny() =>
            new(IdentityAuthorizationDecision.Denied);

        /// <summary>Creates a technical-failure result with a stable typed failure code.</summary>
        public static IdentityAuthorizationResult Failure(
            IdentityAuthorizationFailureCode code,
            RbacAuthorizationFailureCode? rbacFailureCode = null) =>
            new(IdentityAuthorizationDecision.TechnicalFailure, code, rbacFailureCode);
    }
}
