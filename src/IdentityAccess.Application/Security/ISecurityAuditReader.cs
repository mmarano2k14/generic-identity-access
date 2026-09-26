using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Security
{
    /// <summary>Reads persisted security-audit records from one already resolved database route.</summary>
    public interface ISecurityAuditReader
    {
        /// <summary>Lists a bounded, newest-first window for one identity scope and application.</summary>
        Task<IReadOnlyList<SecurityAuditRecord>> ListAsync(
            ResolvedDatabaseRoute route,
            ApplicationKey application,
            SecurityAuditQuery query,
            CancellationToken cancellationToken);
    }
}
