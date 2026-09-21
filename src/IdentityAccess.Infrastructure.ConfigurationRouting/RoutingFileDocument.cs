using System.Text.Json.Serialization;

namespace IdentityAccess.Infrastructure.ConfigurationRouting;

// Deserialization-only types. Mutable input objects are never retained by an activated provider.
internal sealed class RoutingFileDocument
{
    public RoutingFileDocument() { }
    [JsonRequired] public string? SchemaVersion { get; init; }
    [JsonRequired] public long Revision { get; init; }
    [JsonRequired] public string? RoutingProvider { get; init; }
    [JsonRequired] public string? PlacementGranularity { get; init; }
    [JsonRequired] public DestinationDocument?[]? Destinations { get; init; }
    [JsonRequired] public RouteDocument?[]? Routes { get; init; }
    [JsonRequired] public AuthenticationContextDocument?[]? AuthenticationContexts { get; init; }
}

internal sealed class DestinationDocument
{
    public DestinationDocument() { }
    [JsonRequired] public string? Key { get; init; }
    [JsonRequired] public string? Provider { get; init; }
    [JsonRequired] public string? ConnectionSecretRef { get; init; }
    [JsonRequired] public string? State { get; init; }
}

internal sealed class RouteDocument
{
    public RouteDocument() { }
    [JsonRequired] public string? ApplicationKey { get; init; }
    [JsonRequired] public string? IdentityScopeId { get; init; }
    [JsonRequired] public string? DataSet { get; init; }
    [JsonRequired] public string? DestinationKey { get; init; }
    [JsonRequired] public long Version { get; init; }
    [JsonRequired] public string? State { get; init; }
}

internal sealed class AuthenticationContextDocument
{
    public AuthenticationContextDocument() { }
    [JsonRequired] public string? Key { get; init; }
    [JsonRequired] public string? ApplicationKey { get; init; }
    [JsonRequired] public string? IdentityScopeId { get; init; }
    [JsonRequired] public string? State { get; init; }
}
