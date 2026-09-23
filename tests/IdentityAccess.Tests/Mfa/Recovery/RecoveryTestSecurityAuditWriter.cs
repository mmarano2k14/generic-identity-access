using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Tests.Mfa.Recovery
{
    internal sealed class RecoveryTestSecurityAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];

        public Task<bool> TryWriteAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(auditEvent);
            return Task.FromResult(true);
        }
    }
}
