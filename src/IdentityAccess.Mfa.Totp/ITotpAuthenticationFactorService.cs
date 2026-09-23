using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Enrolls, confirms, and verifies time-based one-time password authenticators.</summary>
    public interface ITotpAuthenticationFactorService
    {
        /// <summary>Creates a pending authenticator and returns the secret once for provisioning.</summary>
        Task<TotpEnrollment> BeginEnrollmentAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            string displayName,
            string accountName,
            CancellationToken cancellationToken);

        /// <summary>Confirms a pending authenticator using its first valid code.</summary>
        Task<TotpConfirmationResult> ConfirmEnrollmentAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            string code,
            CancellationToken cancellationToken);

        /// <summary>Verifies a code for an active authenticator and consumes the accepted time step.</summary>
        Task<TotpVerificationResult> VerifyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            string code,
            CancellationToken cancellationToken);
    }
}
