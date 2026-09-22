using System.Text.Json.Serialization;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

    internal sealed class DestinationDocument
    {
        public DestinationDocument() { }
        [JsonRequired] public string? Key { get; init; }
        [JsonRequired] public string? Provider { get; init; }
        [JsonRequired] public string? ConnectionSecretRef { get; init; }
        [JsonRequired] public string? State { get; init; }
    }
}
