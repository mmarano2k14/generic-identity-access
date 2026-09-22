using IdentityAccess.Domain;

namespace IdentityAccess.Application.Routing
{

    /// <summary>Server-side directory location; not an authentication result and not a client DTO.</summary>
    public sealed class AuthenticationDirectoryLocation
    {
        /// <summary>Gets the authentication context key.</summary>
        public string AuthenticationContextKey { get; }
        /// <summary>Gets the route.</summary>
        public ResolvedDatabaseRoute Route { get; }

        /// <summary>Initializes a new instance of <see cref="AuthenticationDirectoryLocation"/>.</summary>
        public AuthenticationDirectoryLocation(string authenticationContextKey, ResolvedDatabaseRoute route)
        {
            _ = new ApplicationKey(authenticationContextKey);
            ArgumentNullException.ThrowIfNull(route);
            AuthenticationContextKey = authenticationContextKey;
            Route = route;
        }

        /// <inheritdoc />
        public override string ToString() => "AuthenticationDirectoryLocation [not authenticated]";
    }
}
