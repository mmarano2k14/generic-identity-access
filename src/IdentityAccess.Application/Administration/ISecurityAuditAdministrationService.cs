using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Defines authorized application operations for security-audit administration reads.</summary>
    public interface ISecurityAuditAdministrationService
    {
        /// <summary>Lists a bounded security-audit window for the requested application context.</summary>
        Task<IReadOnlyList<SecurityAuditRecord>> ListAsync(
            Guid identityScopeId,
            ApplicationKey application,
            SecurityAuditQuery query,
            CancellationToken cancellationToken);
    }
}
