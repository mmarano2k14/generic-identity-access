using IdentityAccess.Domain;

namespace IdentityAccess.Application.Security
{
    /// <summary>Represents one persisted, secret-safe security audit record.</summary>
    public sealed record SecurityAuditRecord
    {
        /// <summary>Gets the durable event identifier.</summary>
        public Guid EventId { get; }
        /// <summary>Gets when the event was persisted.</summary>
        public DateTimeOffset OccurredAt { get; }
        /// <summary>Gets the semantic event type.</summary>
        public SecurityAuditEventType EventType { get; }
        /// <summary>Gets the event outcome.</summary>
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
        /// <summary>Gets the optional categorical reason.</summary>
        public SecurityAuditReasonCode? ReasonCode { get; }
        /// <summary>Gets the optional distributed-trace correlation identifier.</summary>
        public string? CorrelationId { get; }

        /// <summary>Initializes one persisted audit record.</summary>
        public SecurityAuditRecord(
            Guid eventId,
            DateTimeOffset occurredAt,
            SecurityAuditEventType eventType,
            SecurityAuditOutcome outcome,
            Guid identityScopeId,
            Guid? tenantId,
            Guid? userId,
            ApplicationKey? application,
            string? clientId,
            string? targetId,
            SecurityAuditReasonCode? reasonCode,
            string? correlationId)
        {
            if (eventId == Guid.Empty) throw new ArgumentException("Event id must not be empty.", nameof(eventId));
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope id must not be empty.", nameof(identityScopeId));
            if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id must not be empty when supplied.", nameof(tenantId));
            if (userId == Guid.Empty) throw new ArgumentException("User id must not be empty when supplied.", nameof(userId));
            if (clientId is { Length: > 128 }) throw new ArgumentException("Client id must not exceed 128 characters.", nameof(clientId));
            if (targetId is { Length: > 128 }) throw new ArgumentException("Target id must not exceed 128 characters.", nameof(targetId));
            if (correlationId is { Length: > 64 }) throw new ArgumentException("Correlation id must not exceed 64 characters.", nameof(correlationId));

            EventId = eventId;
            OccurredAt = occurredAt;
            EventType = eventType;
            Outcome = outcome;
            IdentityScopeId = identityScopeId;
            TenantId = tenantId;
            UserId = userId;
            Application = application;
            ClientId = clientId;
            TargetId = targetId;
            ReasonCode = reasonCode;
            CorrelationId = correlationId;
        }
    }
}
