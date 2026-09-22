using IdentityAccess.Domain;

namespace IdentityAccess.Application.Security
{
    /// <summary>
    /// Represents a security event using identifiers and categorical metadata only. The contract
    /// deliberately excludes passwords, hashes, session tokens, connection data, and arbitrary
    /// payloads.
    /// </summary>
    public sealed record SecurityAuditEvent
    {
        /// <summary>Gets the event type.</summary>
        public SecurityAuditEventType EventType { get; }
        /// <summary>Gets the outcome.</summary>
        public SecurityAuditOutcome Outcome { get; }
        /// <summary>Gets the identity scope.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the optional tenant identifier.</summary>
        public Guid? TenantId { get; }
        /// <summary>Gets the optional user identifier.</summary>
        public Guid? UserId { get; }
        /// <summary>Gets the optional application context.</summary>
        public ApplicationKey? Application { get; }
        /// <summary>Gets the optional registered client identifier.</summary>
        public string? ClientId { get; }
        /// <summary>Gets the optional safe target identifier.</summary>
        public string? TargetId { get; }
        /// <summary>Gets the optional reason category.</summary>
        public SecurityAuditReasonCode? ReasonCode { get; }

        /// <summary>Initializes a new security audit event.</summary>
        public SecurityAuditEvent(
            SecurityAuditEventType eventType,
            SecurityAuditOutcome outcome,
            Guid identityScopeId,
            Guid? tenantId = null,
            Guid? userId = null,
            ApplicationKey? application = null,
            string? clientId = null,
            string? targetId = null,
            SecurityAuditReasonCode? reasonCode = null)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("An identity scope is required.", nameof(identityScopeId));
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant id must not be empty when supplied.", nameof(tenantId));
            if (userId == Guid.Empty)
                throw new ArgumentException("User id must not be empty when supplied.", nameof(userId));
            if (clientId is { Length: > 128 })
                throw new ArgumentException("Client id must not exceed 128 characters.", nameof(clientId));
            if (targetId is { Length: > 128 })
                throw new ArgumentException("Audit target id must not exceed 128 characters.", nameof(targetId));

            EventType = eventType;
            Outcome = outcome;
            IdentityScopeId = identityScopeId;
            TenantId = tenantId;
            UserId = userId;
            Application = application;
            ClientId = clientId;
            TargetId = targetId;
            ReasonCode = reasonCode;
        }
    }
}
