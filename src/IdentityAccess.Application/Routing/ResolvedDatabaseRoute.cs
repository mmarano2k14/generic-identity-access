using System.Text.Json.Serialization;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Routing
{

    /// <summary>
    /// Immutable server-only placement snapshot. This is neither a permission nor a live connection.
    /// Keep this instance for the whole operation instead of resolving again between reads and writes.
    /// </summary>
    public sealed class ResolvedDatabaseRoute
    {
        /// <summary>Gets the request.</summary>
        public DatabaseRouteRequest Request { get; }
        /// <summary>Gets the destination key.</summary>
        public string DestinationKey { get; }
        /// <summary>Gets the provider.</summary>
        public string Provider => "postgresql";
        /// <summary>Gets the administrative state.</summary>
        public string AdministrativeState => "active";
        /// <summary>Gets the configuration revision.</summary>
        public long ConfigurationRevision { get; }
        /// <summary>Gets the route version.</summary>
        public long RouteVersion { get; }

        /// <summary>Gets the connection secret reference.</summary>
        [JsonIgnore]
        public ConnectionSecretReference ConnectionSecretReference { get; }

        /// <summary>Initializes a new instance of <see cref="ResolvedDatabaseRoute"/>.</summary>
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

        /// <inheritdoc />
        public override string ToString() => "ResolvedDatabaseRoute [server-only placement]";
    }
}
