namespace IdentityAccess.Rbac
{
    /// <summary>Represents the result of RBAC authorization.</summary>
    public sealed record RbacAuthorizationResult(
        RbacAuthorizationDecision Decision,
        RbacAuthorizationFailureCode? FailureCode = null,
        string? DiagnosticDetail = null)
    {
        /// <summary>Creates an allowed authorization result.</summary>
        public static RbacAuthorizationResult Allow() =>
            new(RbacAuthorizationDecision.Allowed);

        /// <summary>Creates a denied authorization result.</summary>
        public static RbacAuthorizationResult Deny() =>
            new(RbacAuthorizationDecision.Denied);

        /// <summary>
        /// Creates a technical-failure result with a stable typed failure code and an optional
        /// internal diagnostic detail.
        /// </summary>
        public static RbacAuthorizationResult Failure(
            RbacAuthorizationFailureCode code,
            string? diagnosticDetail = null) =>
            new(RbacAuthorizationDecision.TechnicalFailure, code, diagnosticDetail);
    }
}
