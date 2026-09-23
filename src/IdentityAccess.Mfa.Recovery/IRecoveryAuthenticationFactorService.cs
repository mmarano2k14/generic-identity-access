using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Generates, replaces, and consumes single-use recovery-code sets.</summary>
    public interface IRecoveryAuthenticationFactorService
    {
        /// <summary>
        /// Generates a new recovery-code set. Any existing active recovery-code set for the user
        /// is revoked atomically before the new set becomes active. Raw codes are returned only by
        /// this operation and are never persisted.
        /// </summary>
        Task<RecoveryCodeSet> GenerateOrReplaceAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            string displayName,
            CancellationToken cancellationToken);

        /// <summary>Verifies and atomically consumes one recovery code.</summary>
        Task<RecoveryCodeVerificationResult> VerifyAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            Guid authenticatorId,
            string code,
            CancellationToken cancellationToken);
    }
}
