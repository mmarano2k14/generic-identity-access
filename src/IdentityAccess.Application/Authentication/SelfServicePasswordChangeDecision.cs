namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines stable outcomes for authenticated self-service password changes.</summary>
    public enum SelfServicePasswordChangeDecision
    {
        /// <summary>The password was changed and all existing sessions were revoked.</summary>
        Succeeded = 1,
        /// <summary>The local session is no longer valid for the requested operation.</summary>
        InvalidSession = 2,
        /// <summary>The supplied current password was invalid.</summary>
        InvalidCurrentPassword = 3,
        /// <summary>The current MFA policy requires a recent multi-factor proof.</summary>
        RecentMfaRequired = 4,
        /// <summary>The replacement password matches the existing password.</summary>
        PasswordReuseRejected = 5,
        /// <summary>The password credential could not be loaded for the authenticated subject.</summary>
        CredentialUnavailable = 6,
        /// <summary>The credential changed concurrently before the replacement could be persisted.</summary>
        ConcurrencyConflict = 7
    }
}
