using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    internal sealed class RouteResolverFake : IDatabaseRouteResolver
    {
        private readonly Exception? _exception;
        public ResolvedDatabaseRoute Route { get; }
        public int CallCount { get; private set; }

        public RouteResolverFake(ResolvedDatabaseRoute route) => Route = route;

        public RouteResolverFake(Exception exception)
        {
            _exception = exception;
            var request = IdentityAuthorizationServiceTests.Request();
            Route = IdentityAuthorizationServiceTests.Route(request);
        }

        public ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            DatabaseRouteRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            if (_exception is not null) throw _exception;
            return ValueTask.FromResult(Route);
        }
    }
}
