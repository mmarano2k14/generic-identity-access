using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Provides administrative bulk revocation of local authentication sessions.
    /// </summary>
    public sealed class SessionAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IAuthenticationClientRegistry clients,
        IAuthenticationSessionStore sessions,
        TimeProvider timeProvider,
        ISecurityAuditWriter auditWriter) : ISessionAdministrationService
    {
        /// <inheritdoc />
        public async Task<int> RevokeUserSessionsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var subject = new SubjectReference(identityScopeId, userId);
            var route = await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId),
                cancellationToken).ConfigureAwait(false);

            var count = await sessions.RevokeAllForSubjectAsync(
                route,
                subject,
                timeProvider.GetUtcNow(),
                cancellationToken).ConfigureAwait(false);

            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.UserSessionsRevoked,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    userId: userId,
                    application: application,
                    targetId: userId.ToString("D")),
                cancellationToken).ConfigureAwait(false);

            return count;
        }

        /// <inheritdoc />
        public async Task<int> RevokeClientSessionsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            string clientId,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

            if (!clients.TryGet(clientId, out var client) ||
                client.Application != application)
            {
                throw new ArgumentException(
                    "The authentication client is not registered for the requested application.",
                    nameof(clientId));
            }

            var route = await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId),
                cancellationToken).ConfigureAwait(false);

            var count = await sessions.RevokeAllForClientAsync(
                route,
                clientId,
                timeProvider.GetUtcNow(),
                cancellationToken).ConfigureAwait(false);

            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.ClientSessionsRevoked,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    application: application,
                    clientId: clientId,
                    targetId: clientId),
                cancellationToken).ConfigureAwait(false);

            return count;
        }
    }
}
