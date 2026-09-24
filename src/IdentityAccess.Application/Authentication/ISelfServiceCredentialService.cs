namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines authenticated self-service credential operations.</summary>
    public interface ISelfServiceCredentialService
    {
        /// <summary>
        /// Changes the password for the exact authenticated local session after re-validating the
        /// current password and any recent-MFA requirement imposed by the current policy.
        /// </summary>
        Task<SelfServicePasswordChangeDecision> ChangePasswordAsync(
            AuthenticatedSessionContext session,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken);
    }
}
