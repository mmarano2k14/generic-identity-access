using IdentityAccess.Application.Routing;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class TenantGroupAssignmentFakeRouteResolver(ResolvedDatabaseRoute route) : IDatabaseRouteResolver
    {
        public ValueTask<ResolvedDatabaseRoute> ResolveAsync(DatabaseRouteRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(route);
        }
    }
}
