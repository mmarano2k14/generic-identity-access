using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Tests.Mfa.Totp
{
    internal sealed class TotpTestSecurityAuditWriter : ISecurityAuditWriter
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
