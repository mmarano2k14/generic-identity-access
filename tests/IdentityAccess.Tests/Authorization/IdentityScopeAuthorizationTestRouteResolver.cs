using IdentityAccess.Application.Routing;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>Provides a deterministic immutable route for identity-scope authorization tests.</summary>
    internal sealed class IdentityScopeAuthorizationTestRouteResolver : IDatabaseRouteResolver
    {
        /// <inheritdoc />
        public ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            DatabaseRouteRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(
                new ResolvedDatabaseRoute(
                    request,
                    "test",
                    new ConnectionSecretReference(
                        "env:TEST_CONNECTION"),
                    configurationRevision: 1,
                    routeVersion: 1));
        }
    }
}
