using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityAccess.Infrastructure.ConfigurationRouting;

internal static class RoutingConfigurationReader
{
    internal const int MaximumBytes = 1_048_576;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        MaxDepth = 16
    };

    internal static RoutingFileDocument Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > MaximumBytes || Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            throw new RoutingConfigurationException(RoutingConfigurationFailure.ConfigurationTooLarge);
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            RejectDuplicateProperties(document.RootElement);
            return document.RootElement.Deserialize<RoutingFileDocument>(SerializerOptions)
                ?? throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidJson);
        }
        catch (JsonException)
        {
            // System.Text.Json exceptions can contain input property names; do not propagate them.
            throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidJson);
        }
    }

    internal static string ReadFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            // Bounded even if another process grows the file while it is read.
            var bytes = new byte[MaximumBytes + 1];
            var length = 0;
            while (length < bytes.Length)
            {
                var read = stream.Read(bytes, length, bytes.Length - length);
                if (read == 0) break;
                length += read;
            }
            if (length > MaximumBytes)
                throw new RoutingConfigurationException(RoutingConfigurationFailure.ConfigurationTooLarge);
            var offset = length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            return new UTF8Encoding(false, true).GetString(bytes, offset, length - offset);
        }
        catch (DecoderFallbackException)
        {
            throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidJson);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            SecurityException or NotSupportedException or ArgumentException)
        {
            throw new RoutingConfigurationException(RoutingConfigurationFailure.FileUnavailable);
        }
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                // Property.Name is decoded, so an escaped spelling cannot hide a duplicate.
                if (!names.Add(property.Name))
                    throw new RoutingConfigurationException(RoutingConfigurationFailure.DuplicateJsonProperty);
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                RejectDuplicateProperties(item);
        }
    }
}
