using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using Microsoft.Extensions.DependencyInjection;
using IdentityApplicationKey = IdentityAccess.Domain.ApplicationKey;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Bridges OrganisationProfile semantic events into the existing routed Identity Access audit
    /// sink. Audit failure never replaces a successful primary OrganisationProfile mutation.
    /// </summary>
    internal sealed class OrganisationProfileSecurityAuditWriter(
        IServiceProvider services,
        ILogger<OrganisationProfileSecurityAuditWriter> logger)
        : IOrganisationProfileSecurityAuditWriter
    {
        /// <inheritdoc />
        public async Task<bool> TryWriteAsync(
            Guid identityScopeId,
            Guid? tenantId,
            string applicationKey,
            SecurityAuditEventType eventType,
            string targetId,
            CancellationToken cancellationToken)
        {
            if (identityScopeId == Guid.Empty ||
                tenantId == Guid.Empty ||
                string.IsNullOrWhiteSpace(applicationKey) ||
                string.IsNullOrWhiteSpace(targetId))
            {
                logger.LogWarning(
                    "OrganisationProfile security audit event {EventType} was skipped because its trusted identifiers were incomplete.",
                    eventType);
                return false;
            }

            var routeResolver = services.GetService<IDatabaseRouteResolver>();
            var auditWriter = services.GetService<ISecurityAuditWriter>();

            if (routeResolver is null || auditWriter is null)
            {
                logger.LogWarning(
                    "OrganisationProfile security audit event {EventType} could not be persisted because the Identity Access audit pipeline is unavailable.",
                    eventType);
                return false;
            }

            try
            {
                var application = new IdentityApplicationKey(applicationKey);

                var route = await routeResolver.ResolveAsync(
                        new DatabaseRouteRequest(
                            application,
                            identityScopeId),
                        cancellationToken)
                    .ConfigureAwait(false);

                return await auditWriter.TryWriteAsync(
                        route,
                        new SecurityAuditEvent(
                            eventType,
                            SecurityAuditOutcome.Succeeded,
                            identityScopeId,
                            tenantId,
                            application: application,
                            targetId: targetId),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "OrganisationProfile security audit event {EventType} could not be persisted. The primary mutation result is preserved.",
                    eventType);
                return false;
            }
        }
    }
}
