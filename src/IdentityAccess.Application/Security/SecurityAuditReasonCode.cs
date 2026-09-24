namespace IdentityAccess.Application.Security
{
    /// <summary>Defines safe categorical reasons for security audit events.</summary>
    public enum SecurityAuditReasonCode
    {
        /// <summary>The supplied credentials were invalid.</summary>
        InvalidCredentials = 1,
        /// <summary>The identity directory was unavailable.</summary>
        DirectoryUnavailable = 2,
        /// <summary>The supplied session was invalid.</summary>
        InvalidSession = 3,
        /// <summary>The supplied authentication-factor proof was invalid.</summary>
        InvalidAuthenticationFactorProof = 4,
        /// <summary>An authentication-factor proof reused an already consumed replay boundary.</summary>
        AuthenticationFactorReplayDetected = 5,
        /// <summary>A sensitive operation requires a recent multi-factor proof.</summary>
        RecentAuthenticationRequired = 6,
        /// <summary>The proposed replacement password matches the current password.</summary>
        PasswordReuseRejected = 7
    }
}
