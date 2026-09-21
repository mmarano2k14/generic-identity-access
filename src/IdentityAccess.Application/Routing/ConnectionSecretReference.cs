using System.Text.Json.Serialization;

namespace IdentityAccess.Application.Routing;

/// <summary>An opaque secret-store reference, never the connection string itself.</summary>
public sealed record ConnectionSecretReference
{
    [JsonIgnore]
    public string Value { get; }

    public ConnectionSecretReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var separator = value.IndexOf(':');
        if (value.Length > 256 || separator is < 1 or > 32 || separator == value.Length - 1 ||
            value[0] is < 'a' or > 'z' ||
            value[..separator].Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')) ||
            value[(separator + 1)..].Any(c => !(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or
                >= '0' and <= '9' or '_' or '-' or '/')))
            throw new ArgumentException("A secret reference must use a scheme and an opaque identifier.", nameof(value));
        Value = value;
    }

    public override string ToString() => "[connection-secret-reference]";
}
