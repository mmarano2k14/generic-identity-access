using IdentityAccess.Application.Security;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Secret-safe security-audit response exposed to authorized administrators.</summary>
    public sealed record SecurityAuditEventResponse(
        Guid EventId,
        DateTimeOffset OccurredAt,
        string EventType,
        string Outcome,
        Guid IdentityScopeId,
        Guid? TenantId,
        Guid? UserId,
        string? ApplicationKey,
        string? ClientId,
        string? TargetId,
        string? ReasonCode,
        string? CorrelationId)
    {
        /// <summary>Maps the application-layer audit read model into the public API response.</summary>
        public static SecurityAuditEventResponse From(SecurityAuditRecord record) =>
            new(
                record.EventId,
                record.OccurredAt,
                record.EventType.ToString(),
                record.Outcome.ToString(),
                record.IdentityScopeId,
                record.TenantId,
                record.UserId,
                record.Application?.Value,
                record.ClientId,
                record.TargetId,
                record.ReasonCode?.ToString(),
                record.CorrelationId);
    }
}
