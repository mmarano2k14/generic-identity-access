using IdentityAccess.Application.Routing;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class EffectiveAdministrationContextFakeRouteResolver : IDatabaseRouteResolver
    {
        public int CallCount { get; private set; }

        public ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            DatabaseRouteRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(new ResolvedDatabaseRoute(
                request, "identity-a", new ConnectionSecretReference("env:IDENTITY_TEST"), 1, 1));
        }
    }
}
