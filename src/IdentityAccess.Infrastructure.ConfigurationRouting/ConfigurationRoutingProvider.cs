using System.Collections.Frozen;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Infrastructure.ConfigurationRouting;

/// <summary>
/// A single validated, immutable configuration revision. No file watching, merge, cache fallback,
/// database connection, identity verification or implicit sharing is performed by this provider.
/// </summary>
public sealed class ConfigurationRoutingProvider : IDatabaseRouteResolver, IAuthenticationDirectoryLocator
{
    public const int MaximumDestinations = 256;
    public const int MaximumRoutes = 4096;
    public const int MaximumAuthenticationContexts = 4096;
    public const int MaximumConfigurationBytes = 1_048_576;

    private readonly FrozenDictionary<string, DestinationEntry> _destinations;
    private readonly FrozenDictionary<RouteKey, RouteEntry> _routes;
    private readonly FrozenDictionary<string, AuthenticationEntry> _authenticationContexts;

    public long ConfigurationRevision { get; }
    public int DestinationCount => _destinations.Count;
    public int RouteCount => _routes.Count;
    public int AuthenticationContextCount => _authenticationContexts.Count;

    private ConfigurationRoutingProvider(long revision,
        Dictionary<string, DestinationEntry> destinations,
        Dictionary<RouteKey, RouteEntry> routes,
        Dictionary<string, AuthenticationEntry> authenticationContexts)
    {
        ConfigurationRevision = revision;
        _destinations = destinations.ToFrozenDictionary(StringComparer.Ordinal);
        _routes = routes.ToFrozenDictionary();
        _authenticationContexts = authenticationContexts.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public static ConfigurationRoutingProvider LoadFile(string filePath) =>
        Parse(RoutingConfigurationReader.ReadFile(filePath));

    public static ConfigurationRoutingProvider Parse(string json)
    {
        var document = RoutingConfigurationReader.Read(json);
        Require(document.SchemaVersion == "1", RoutingConfigurationFailure.UnsupportedSchema);
        Require(document.RoutingProvider == "configuration", RoutingConfigurationFailure.UnsupportedProvider);
        Require(document.PlacementGranularity == "identity-scope",
            RoutingConfigurationFailure.UnsupportedPlacementGranularity);
        Require(document.Revision > 0);
        var destinationDocuments = document.Destinations
            ?? throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);
        var routeDocuments = document.Routes
            ?? throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);
        var authenticationDocuments = document.AuthenticationContexts
            ?? throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);
        Require(destinationDocuments.Length is > 0 and <= MaximumDestinations);
        Require(routeDocuments.Length is > 0 and <= MaximumRoutes);
        Require(authenticationDocuments.Length <= MaximumAuthenticationContexts);

        var destinations = new Dictionary<string, DestinationEntry>(StringComparer.Ordinal);
        foreach (var candidate in destinationDocuments)
        {
            if (candidate is null) throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);
            var key = Key(candidate.Key);
            Require(candidate.Provider == "postgresql", RoutingConfigurationFailure.UnsupportedProvider);
            var entry = new DestinationEntry(key, Secret(candidate.ConnectionSecretRef), Active(candidate.State));
            Require(destinations.TryAdd(key, entry), RoutingConfigurationFailure.DuplicateDestination);
        }

        var routes = new Dictionary<RouteKey, RouteEntry>();
        var placements = new Dictionary<Guid, string>();
        foreach (var candidate in routeDocuments)
        {
            if (candidate is null) throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);
            var application = Key(candidate.ApplicationKey);
            var scope = Scope(candidate.IdentityScopeId);
            Require(candidate.DataSet == "identity-directory");
            var destination = Key(candidate.DestinationKey);
            Require(candidate.Version > 0);
            Require(destinations.ContainsKey(destination), RoutingConfigurationFailure.UnknownDestination);
            var key = new RouteKey(application, scope, IdentityDataSet.IdentityDirectory);
            var entry = new RouteEntry(destination, candidate.Version, Active(candidate.State));
            Require(routes.TryAdd(key, entry), RoutingConfigurationFailure.DuplicateRoute);
            // Even disabled registrations cannot describe conflicting authorities for the same directory.
            if (placements.TryGetValue(scope, out var previous))
                Require(previous == destination, RoutingConfigurationFailure.ConflictingScopePlacement);
            else
                placements.Add(scope, destination);
        }

        var authenticationContexts = new Dictionary<string, AuthenticationEntry>(StringComparer.Ordinal);
        foreach (var candidate in authenticationDocuments)
        {
            if (candidate is null) throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);
            var key = Key(candidate.Key);
            var application = Key(candidate.ApplicationKey);
            var scope = Scope(candidate.IdentityScopeId);
            var routeKey = new RouteKey(application, scope, IdentityDataSet.IdentityDirectory);
            Require(routes.ContainsKey(routeKey), RoutingConfigurationFailure.UnknownAuthenticationRoute);
            var entry = new AuthenticationEntry(application, scope, Active(candidate.State));
            Require(authenticationContexts.TryAdd(key, entry), RoutingConfigurationFailure.DuplicateAuthenticationContext);
        }

        return new ConfigurationRoutingProvider(document.Revision, destinations, routes, authenticationContexts);
    }

    public ValueTask<ResolvedDatabaseRoute> ResolveAsync(DatabaseRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        return ValueTask.FromResult(Resolve(request));
    }

    public ValueTask<AuthenticationDirectoryLocation> LocateAsync(ApplicationKey expectedApplication,
        string authenticationContextKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(expectedApplication);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationContextKey);
        if (!_authenticationContexts.TryGetValue(authenticationContextKey, out var entry))
            throw new DatabaseRouteException(DatabaseRouteFailure.AuthenticationContextNotFound);
        if (entry.Application != expectedApplication.Value)
            throw new DatabaseRouteException(DatabaseRouteFailure.ApplicationContextMismatch);
        if (!entry.Active)
            throw new DatabaseRouteException(DatabaseRouteFailure.AuthenticationContextDisabled);
        var route = Resolve(new DatabaseRouteRequest(expectedApplication, entry.Scope));
        return ValueTask.FromResult(new AuthenticationDirectoryLocation(authenticationContextKey, route));
    }

    private ResolvedDatabaseRoute Resolve(DatabaseRouteRequest request)
    {
        var key = new RouteKey(request.Application.Value, request.IdentityScopeId, request.DataSet);
        if (!_routes.TryGetValue(key, out var route))
            throw new DatabaseRouteException(DatabaseRouteFailure.RouteNotFound);
        if (!route.Active)
            throw new DatabaseRouteException(DatabaseRouteFailure.RouteDisabled);
        var destination = _destinations[route.Destination];
        if (!destination.Active)
            throw new DatabaseRouteException(DatabaseRouteFailure.DestinationDisabled);
        return new ResolvedDatabaseRoute(request, destination.Key, destination.Secret,
            ConfigurationRevision, route.Version);
    }

    private static void Require(bool condition,
        RoutingConfigurationFailure code = RoutingConfigurationFailure.InvalidValue)
    {
        if (!condition) throw new RoutingConfigurationException(code);
    }

    private static string Key(string? value)
    {
        try { return new ApplicationKey(value!).Value; }
        catch (ArgumentException) { throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue); }
    }

    private static Guid Scope(string? value)
    {
        if (!Guid.TryParseExact(value, "D", out var scope) || scope == Guid.Empty)
            throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);
        return scope;
    }

    private static ConnectionSecretReference Secret(string? value)
    {
        try { return new ConnectionSecretReference(value!); }
        catch (ArgumentException) { throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue); }
    }

    private static bool Active(string? value) => value switch
    {
        "active" => true,
        "disabled" => false,
        _ => throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue)
    };

    private readonly record struct RouteKey(string Application, Guid Scope, IdentityDataSet DataSet);
    private sealed record DestinationEntry(string Key, ConnectionSecretReference Secret, bool Active);
    private sealed record RouteEntry(string Destination, long Version, bool Active);
    private sealed record AuthenticationEntry(string Application, Guid Scope, bool Active);
}
