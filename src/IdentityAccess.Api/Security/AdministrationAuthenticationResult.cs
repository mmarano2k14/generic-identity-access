namespace IdentityAccess.Api.Security
{
    /// <summary>Represents trusted administration-context resolution.</summary>
    public readonly record struct AdministrationAuthenticationResult(
        AdministrationAuthenticationDecision Decision,
        AdministrationRequestContext? Context = null,
        AdministrationAuthenticationFailureCode? FailureCode = null)
    {
        /// <summary>Creates an authenticated result.</summary>
        public static AdministrationAuthenticationResult Authenticated(
            AdministrationRequestContext context) =>
            new(
                AdministrationAuthenticationDecision.Authenticated,
                context);

        /// <summary>Creates an unauthenticated result.</summary>
        public static AdministrationAuthenticationResult Unauthenticated(
            AdministrationAuthenticationFailureCode failureCode) =>
            new(
                AdministrationAuthenticationDecision.Unauthenticated,
                FailureCode: failureCode);

        /// <summary>Creates an unavailable result.</summary>
        public static AdministrationAuthenticationResult Unavailable(
            AdministrationAuthenticationFailureCode failureCode) =>
            new(
                AdministrationAuthenticationDecision.Unavailable,
                FailureCode: failureCode);
    }
}
