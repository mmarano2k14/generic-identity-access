using IdentityAccess.Application.Routing;

namespace IdentityAccess.Tests.Mfa.Totp
{
    internal sealed class TotpTestRouteResolver : IDatabaseRouteResolver
    {
        public ValueTask<ResolvedDatabaseRoute> ResolveAsync(
            DatabaseRouteRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                new ResolvedDatabaseRoute(
                    request,
                    "identity-test",
                    new ConnectionSecretReference("env:test"),
                    configurationRevision: 1,
                    routeVersion: 1));
        }
    }
}
