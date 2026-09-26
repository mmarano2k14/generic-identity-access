namespace IdentityAccess.Application.Security
{
    /// <summary>Defines bounded, secret-safe filters for persisted security audit reads.</summary>
    public sealed record SecurityAuditQuery
    {
        /// <summary>Gets the optional tenant filter.</summary>
        public Guid? TenantId { get; }
        /// <summary>Gets the optional user filter.</summary>
        public Guid? UserId { get; }
        /// <summary>Gets the optional semantic event-type filter.</summary>
        public SecurityAuditEventType? EventType { get; }
        /// <summary>Gets the optional outcome filter.</summary>
        public SecurityAuditOutcome? Outcome { get; }
        /// <summary>Gets the optional exact correlation-id filter.</summary>
        public string? CorrelationId { get; }
        /// <summary>Gets the zero-based result offset.</summary>
        public int Offset { get; }
        /// <summary>Gets the bounded page size.</summary>
        public int Limit { get; }

        /// <summary>Initializes audit query filters.</summary>
        public SecurityAuditQuery(
            Guid? tenantId,
            Guid? userId,
            SecurityAuditEventType? eventType,
            SecurityAuditOutcome? outcome,
            string? correlationId,
            int offset,
            int limit)
        {
            if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id must not be empty when supplied.", nameof(tenantId));
            if (userId == Guid.Empty) throw new ArgumentException("User id must not be empty when supplied.", nameof(userId));
            if (correlationId is { Length: > 64 }) throw new ArgumentException("Correlation id must not exceed 64 characters.", nameof(correlationId));
            if (correlationId is not null && correlationId.Length == 0) throw new ArgumentException("Correlation id must not be empty when supplied.", nameof(correlationId));

            TenantId = tenantId;
            UserId = userId;
            EventType = eventType;
            Outcome = outcome;
            CorrelationId = correlationId;
            Offset = offset;
            Limit = limit;
        }
    }
}
