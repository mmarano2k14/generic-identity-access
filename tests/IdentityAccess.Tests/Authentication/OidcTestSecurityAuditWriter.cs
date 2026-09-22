using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Accepts semantic security audit events during OIDC protocol tests.</summary>
    internal sealed class OidcTestSecurityAuditWriter :
        ISecurityAuditWriter
    {
        /// <inheritdoc />
        public Task<bool> TryWriteAsync(
            ResolvedDatabaseRoute route,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }
    }
}
