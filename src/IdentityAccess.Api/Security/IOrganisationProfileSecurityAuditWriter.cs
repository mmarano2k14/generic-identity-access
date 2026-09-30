using IdentityAccess.Application.Security;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Writes safe categorical OrganisationProfile audit events through the existing Identity Access
    /// security-audit pipeline without making OrganisationProfile depend on Identity Access.
    /// </summary>
    public interface IOrganisationProfileSecurityAuditWriter
    {
        /// <summary>Attempts to persist one semantic OrganisationProfile audit event.</summary>
        Task<bool> TryWriteAsync(
            Guid identityScopeId,
            Guid? tenantId,
            string applicationKey,
            SecurityAuditEventType eventType,
            string targetId,
            CancellationToken cancellationToken);
    }
}
