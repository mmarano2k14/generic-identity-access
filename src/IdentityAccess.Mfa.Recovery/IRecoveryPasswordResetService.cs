namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Defines recovery-code-backed password replacement for a registered authentication client.</summary>
    public interface IRecoveryPasswordResetService
    {
        /// <summary>
        /// Verifies and consumes one recovery code, replaces the password, clears lockout state,
        /// and revokes every existing local session and refresh-token family for the subject.
        /// </summary>
        Task<RecoveryPasswordResetDecision> ResetPasswordAsync(
            string clientId,
            string loginIdentifier,
            string recoveryCode,
            string newPassword,
            CancellationToken cancellationToken);
    }
}
