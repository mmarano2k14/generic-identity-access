using System.Text.Json.Serialization;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

    internal sealed class AuthenticationContextDocument
    {
        public AuthenticationContextDocument() { }
        [JsonRequired] public string? Key { get; init; }
        [JsonRequired] public string? ApplicationKey { get; init; }
        [JsonRequired] public string? IdentityScopeId { get; init; }
        [JsonRequired] public string? State { get; init; }
    }
}
