namespace IdentityAccess.Application.Security
{
    /// <summary>Defines the outcome of a security audit event.</summary>
    public enum SecurityAuditOutcome
    {
        /// <summary>The operation succeeded.</summary>
        Succeeded = 1,
        /// <summary>The operation was denied or rejected.</summary>
        Denied = 2,
        /// <summary>The operation failed for a non-authorization reason.</summary>
        Failed = 3
    }
}
