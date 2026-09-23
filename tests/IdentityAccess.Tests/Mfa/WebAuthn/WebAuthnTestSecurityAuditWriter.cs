using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    internal sealed class WebAuthnTestSecurityAuditWriter : ISecurityAuditWriter
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
