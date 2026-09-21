using IdentityAccess.Domain;

namespace IdentityAccess.Application.Routing;

/// <summary>Server-side directory location; not an authentication result and not a client DTO.</summary>
public sealed class AuthenticationDirectoryLocation
{
    public string AuthenticationContextKey { get; }
    public ResolvedDatabaseRoute Route { get; }

    public AuthenticationDirectoryLocation(string authenticationContextKey, ResolvedDatabaseRoute route)
    {
        _ = new ApplicationKey(authenticationContextKey);
        ArgumentNullException.ThrowIfNull(route);
        AuthenticationContextKey = authenticationContextKey;
        Route = route;
    }

    public override string ToString() => "AuthenticationDirectoryLocation [not authenticated]";
}
