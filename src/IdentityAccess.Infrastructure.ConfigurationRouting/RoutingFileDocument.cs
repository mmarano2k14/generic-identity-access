using System.Text.Json.Serialization;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

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
}
