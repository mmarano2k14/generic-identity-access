namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>Provides RFC 4648 URL-safe base64 encoding without padding.</summary>
    internal static class OidcBase64Url
    {
        /// <summary>Encodes binary data as unpadded base64url.</summary>
        public static string Encode(
            ReadOnlySpan<byte> value) =>
            Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }
}
