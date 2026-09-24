namespace IdentityAccess.Application.Authentication
{
    /// <summary>Coordinates server-trusted session assurance inspection and factor-driven upgrades.</summary>
    public interface IAuthenticationAssuranceService
    {
        /// <summary>Evaluates the current persisted session assurance against the current MFA policy.</summary>
        Task<AuthenticationAssuranceEvaluation?> EvaluateAsync(
            AuthenticatedSessionContext session,
            TimeSpan maximumMfaAge,
            CancellationToken cancellationToken);

        /// <summary>
        /// Records one successfully verified additional factor against the exact active session.
        /// Returns null when the session/user/client/application binding is no longer valid.
        /// </summary>
        Task<AuthenticatedSessionContext?> RecordFactorAsync(
            AuthenticatedSessionContext session,
            string factorMethodReference,
            CancellationToken cancellationToken);
    }
}
