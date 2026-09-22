using System.Text.Json.Serialization;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

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
}
