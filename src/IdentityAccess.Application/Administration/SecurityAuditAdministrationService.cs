using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Coordinates bounded security-audit reads without owning persistence or authorization.</summary>
    public sealed class SecurityAuditAdministrationService(
        IDatabaseRouteResolver routeResolver,
        ISecurityAuditReader auditReader) : ISecurityAuditAdministrationService
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<SecurityAuditRecord>> ListAsync(
            Guid identityScopeId,
            ApplicationKey application,
            SecurityAuditQuery query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(query);
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("Identity scope id must not be empty.", nameof(identityScopeId));
            AdministrationPaging.EnsureValid(query.Offset, query.Limit);

            var route = await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId),
                cancellationToken).ConfigureAwait(false);

            return await auditReader.ListAsync(route, application, query, cancellationToken).ConfigureAwait(false);
        }
    }
}
