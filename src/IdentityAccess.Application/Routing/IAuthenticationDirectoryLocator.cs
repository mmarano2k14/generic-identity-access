using IdentityAccess.Domain;

namespace IdentityAccess.Application.Routing
{

    /// <summary>
    /// Locates a registered directory before authentication. The expected application must come from
    /// server integration. A successful lookup does not validate a subject, tenant membership or session.
    /// </summary>
    public interface IAuthenticationDirectoryLocator
    {
        /// <summary>Locates the registered identity directory for the requested authentication context.</summary>
        ValueTask<AuthenticationDirectoryLocation> LocateAsync(ApplicationKey expectedApplication,
            string authenticationContextKey, CancellationToken cancellationToken);
    }
}
