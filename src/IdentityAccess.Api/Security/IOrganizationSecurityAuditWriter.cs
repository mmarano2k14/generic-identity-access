using IdentityAccess.Application.Security;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Writes best-effort semantic Organization Directory security events through the existing
    /// Identity Access audit pipeline without making Organization Directory depend on Identity Access.
    /// </summary>
    public interface IOrganizationSecurityAuditWriter
    {
        /// <summary>Attempts to write one safe categorical Organization Directory audit event.</summary>
        Task<bool> TryWriteAsync(
            Guid identityScopeId,
            Guid tenantId,
            string applicationKey,
            SecurityAuditEventType eventType,
            string targetId,
            CancellationToken cancellationToken);
    }
}
