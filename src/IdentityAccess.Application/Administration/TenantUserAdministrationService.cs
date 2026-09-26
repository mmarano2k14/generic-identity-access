using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Provides bounded tenant-constrained user reads without exposing the scope-wide directory.</summary>
    public sealed class TenantUserAdministrationService(
        IDatabaseRouteResolver routeResolver,
        ITenantUserReadStore tenantUsers)
        : ITenantUserAdministrationService
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<TenantUserReadRecord>> ListAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid tenantId,
            string? search,
            bool activeMembershipsOnly,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            AdministrationPaging.EnsureValid(offset, limit);
            var normalizedSearch = AdministrationSearch.Normalize(search);
            var boundedLimit = AdministrationSearch.Limit(normalizedSearch, limit);
            var route = await routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken)
                .ConfigureAwait(false);

            return await tenantUsers.ListAsync(
                    route,
                    new TenantReference(identityScopeId, tenantId),
                    normalizedSearch,
                    activeMembershipsOnly,
                    offset,
                    boundedLimit,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
