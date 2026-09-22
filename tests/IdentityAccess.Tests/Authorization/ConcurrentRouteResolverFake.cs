using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    internal sealed class ConcurrentRouteResolverFake : IDatabaseRouteResolver
    {
        public System.Collections.Concurrent.ConcurrentBag<Guid> SeenScopes { get; } = [];

        public ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            DatabaseRouteRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SeenScopes.Add(request.IdentityScopeId);
            return ValueTask.FromResult(new ResolvedDatabaseRoute(
                request,
                request.IdentityScopeId == IdentityAuthorizationServiceTests.ScopeA ? "identity-a" : "identity-b",
                new ConnectionSecretReference(request.IdentityScopeId == IdentityAuthorizationServiceTests.ScopeA
                    ? "env:IDENTITY_ACCESS_POSTGRES_A"
                    : "env:IDENTITY_ACCESS_POSTGRES_B"),
                1,
                1));
        }
    }
}
