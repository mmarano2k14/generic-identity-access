using IdentityAccess.Application.Routing;

namespace IdentityAccess.Application.Security
{
    /// <summary>
    /// Writes semantic security events to the resolved identity destination. These events enrich
    /// the transactionally captured persistence-mutation ledger with domain-level meaning.
    /// </summary>
    public interface ISecurityAuditWriter
    {
        /// <summary>
        /// Attempts to persist a semantic event. Semantic-audit sink failure does not replace the
        /// primary operation result. Persisted state mutations are independently covered by the
        /// transactionally co-committed security-mutation ledger.
        /// </summary>
        Task<bool> TryWriteAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken);
    }
}
