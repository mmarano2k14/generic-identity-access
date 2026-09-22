using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authentication
{
    internal sealed class FixedDirectoryLocator(
        ResolvedDatabaseRoute route)
        : IAuthenticationDirectoryLocator
    {
        public ValueTask<AuthenticationDirectoryLocation> LocateAsync(
            ApplicationKey expectedApplication,
            string authenticationContextKey,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(
                new AuthenticationDirectoryLocation(
                    authenticationContextKey,
                    route));
    }
}
