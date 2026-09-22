using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Returns one deterministic identity-directory route for OIDC tests.</summary>
    internal sealed class OidcTestDirectoryLocator(
        Guid identityScopeId,
        ApplicationKey application,
        string authenticationContextKey)
        : IAuthenticationDirectoryLocator
    {
        /// <inheritdoc />
        public ValueTask<AuthenticationDirectoryLocation> LocateAsync(
            ApplicationKey expectedApplication,
            string requestedAuthenticationContextKey,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (expectedApplication != application ||
                !string.Equals(
                    requestedAuthenticationContextKey,
                    authenticationContextKey,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Unexpected OIDC test directory lookup.");
            }

            return ValueTask.FromResult(
                new AuthenticationDirectoryLocation(
                    authenticationContextKey,
                    new ResolvedDatabaseRoute(
                        new DatabaseRouteRequest(
                            application,
                            identityScopeId),
                        "oidc-test",
                        new ConnectionSecretReference(
                            "env:OIDC_TEST_CONNECTION"),
                        configurationRevision: 1,
                        routeVersion: 1)));
        }
    }
}
