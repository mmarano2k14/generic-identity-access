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
        InvalidSession = 3
    }
}
