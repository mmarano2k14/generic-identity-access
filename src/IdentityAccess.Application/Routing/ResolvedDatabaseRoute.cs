using System.Text.Json.Serialization;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Routing;

/// <summary>
/// Immutable server-only placement snapshot. This is neither a permission nor a live connection.
/// Keep this instance for the whole operation instead of resolving again between reads and writes.
/// </summary>
public sealed class ResolvedDatabaseRoute
{
    public DatabaseRouteRequest Request { get; }
    public string DestinationKey { get; }
    public string Provider => "postgresql";
    public string AdministrativeState => "active";
    public long ConfigurationRevision { get; }
    public long RouteVersion { get; }

    [JsonIgnore]
    public ConnectionSecretReference ConnectionSecretReference { get; }

    public ResolvedDatabaseRoute(DatabaseRouteRequest request, string destinationKey,
        ConnectionSecretReference connectionSecretReference, long configurationRevision, long routeVersion)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionSecretReference);
        _ = new ApplicationKey(destinationKey);
        if (configurationRevision <= 0)
            throw new ArgumentOutOfRangeException(nameof(configurationRevision));
        if (routeVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(routeVersion));
        Request = request;
        DestinationKey = destinationKey;
        ConnectionSecretReference = connectionSecretReference;
        ConfigurationRevision = configurationRevision;
        RouteVersion = routeVersion;
    }

    public override string ToString() => "ResolvedDatabaseRoute [server-only placement]";
}
