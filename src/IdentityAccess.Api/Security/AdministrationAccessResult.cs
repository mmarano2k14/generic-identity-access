namespace IdentityAccess.Api.Security
{
    /// <summary>Represents administration access authorization.</summary>
    public readonly record struct AdministrationAccessResult(
        AdministrationAccessDecision Decision,
        AdministrationAccessFailureCode? FailureCode = null)
    {
        /// <summary>Creates an allowed result.</summary>
        public static AdministrationAccessResult Allow() =>
            new(AdministrationAccessDecision.Allowed);

        /// <summary>Creates an unauthenticated result.</summary>
        public static AdministrationAccessResult Unauthenticated(
            AdministrationAccessFailureCode failureCode) =>
            new(
                AdministrationAccessDecision.Unauthenticated,
                failureCode);

        /// <summary>Creates a denied result.</summary>
        public static AdministrationAccessResult Deny(
            AdministrationAccessFailureCode? failureCode = null) =>
            new(
                AdministrationAccessDecision.Denied,
                failureCode);

        /// <summary>Creates an unavailable result.</summary>
        public static AdministrationAccessResult Unavailable(
            AdministrationAccessFailureCode failureCode) =>
            new(
                AdministrationAccessDecision.Unavailable,
                failureCode);
    }
}
