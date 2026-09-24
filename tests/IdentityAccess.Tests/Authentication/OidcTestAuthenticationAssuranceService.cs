using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Returns deterministic assurance decisions for OIDC protocol tests.</summary>
    internal sealed class OidcTestAuthenticationAssuranceService(bool satisfied = true)
        : IAuthenticationAssuranceService
    {
        public Task<AuthenticationAssuranceEvaluation?> EvaluateAsync(
            AuthenticatedSessionContext session,
            TimeSpan maximumMfaAge,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(session);
            if (maximumMfaAge <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maximumMfaAge));

            var fresh = session.Assurance.IsRecentMultiFactor(DateTimeOffset.UtcNow, maximumMfaAge);
            return Task.FromResult<AuthenticationAssuranceEvaluation?>(
                new AuthenticationAssuranceEvaluation(
                    null,
                    session.Assurance,
                    satisfied,
                    fresh));
        }

        public Task<AuthenticatedSessionContext?> RecordFactorAsync(
            AuthenticatedSessionContext session,
            string factorMethodReference,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
