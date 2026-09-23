namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Strict unpadded base64url codec used by the WebAuthn provider boundary.</summary>
    internal static class WebAuthnBase64Url
    {
        internal static string Encode(ReadOnlySpan<byte> value) =>
            Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        internal static bool TryDecode(string value, int maximumBytes, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(value) || value.Contains('=') || maximumBytes <= 0)
                return false;

            foreach (var character in value)
            {
                if (!(character is >= 'A' and <= 'Z' ||
                      character is >= 'a' and <= 'z' ||
                      character is >= '0' and <= '9' ||
                      character is '-' or '_'))
                {
                    return false;
                }
            }

            var padding = (4 - value.Length % 4) % 4;
            var base64 = value.Replace('-', '+').Replace('_', '/') + new string('=', padding);

            try
            {
                bytes = Convert.FromBase64String(base64);
                if (bytes.Length == 0 || bytes.Length > maximumBytes)
                {
                    bytes = Array.Empty<byte>();
                    return false;
                }

                return string.Equals(Encode(bytes), value, StringComparison.Ordinal);
            }
            catch (FormatException)
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }
    }
}
