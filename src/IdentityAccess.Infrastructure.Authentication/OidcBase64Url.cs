namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>Provides strict RFC 4648 URL-safe base64 encoding and decoding without padding.</summary>
    internal static class OidcBase64Url
    {
        /// <summary>Encodes binary data as unpadded base64url.</summary>
        public static string Encode(
            ReadOnlySpan<byte> value) =>
            Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        /// <summary>Attempts to decode strict unpadded base64url input.</summary>
        public static bool TryDecode(
            string value,
            out byte[] decoded)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length % 4 == 1 ||
                value.Any(
                    character =>
                        !((character >= 'A' && character <= 'Z') ||
                            (character >= 'a' && character <= 'z') ||
                            (character >= '0' && character <= '9') ||
                            character == '-' ||
                            character == '_')))
            {
                decoded = [];
                return false;
            }

            var padded =
                value
                    .Replace('-', '+')
                    .Replace('_', '/');

            padded +=
                new string(
                    '=',
                    (4 - padded.Length % 4) % 4);

            try
            {
                decoded =
                    Convert.FromBase64String(
                        padded);

                return true;
            }
            catch (FormatException)
            {
                decoded = [];
                return false;
            }
        }
    }
}
