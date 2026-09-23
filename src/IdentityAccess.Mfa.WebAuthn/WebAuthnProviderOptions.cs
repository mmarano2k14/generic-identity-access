namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Validated server-side settings for the WebAuthn registration profile.</summary>
    internal sealed class WebAuthnProviderOptions
    {
        internal const int ChallengeLengthBytes = 32;
        internal const int UserHandleLengthBytes = 32;
        internal const int CoseAlgorithmEs256 = -7;
        internal const int MinimumChallengeLifetimeSeconds = 60;
        internal const int MaximumChallengeLifetimeSeconds = 600;
        internal const int DefaultChallengeLifetimeSeconds = 300;

        public string RelyingPartyId { get; }
        public string RelyingPartyName { get; }
        public IReadOnlySet<string> AllowedOrigins { get; }
        public int ChallengeLifetimeSeconds { get; }

        public WebAuthnProviderOptions(
            string relyingPartyId,
            string relyingPartyName,
            IEnumerable<string> allowedOrigins,
            int challengeLifetimeSeconds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relyingPartyId);
            ArgumentException.ThrowIfNullOrWhiteSpace(relyingPartyName);
            ArgumentNullException.ThrowIfNull(allowedOrigins);

            var rpInput = relyingPartyId.Trim().TrimEnd('.');
            if (rpInput.Length is 0 or > 253 ||
                rpInput.Contains('/') || rpInput.Contains(':') || rpInput.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException("WebAuthn relying-party identifier must be a host name without scheme or path.", nameof(relyingPartyId));
            }

            string rpId;
            try
            {
                rpId = new System.Globalization.IdnMapping().GetAscii(rpInput).ToLowerInvariant();
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("WebAuthn relying-party identifier is not a valid DNS host name.", nameof(relyingPartyId), exception);
            }

            if (relyingPartyName.Trim().Length > 128)
                throw new ArgumentException("WebAuthn relying-party name must not exceed 128 characters.", nameof(relyingPartyName));

            if (challengeLifetimeSeconds is < MinimumChallengeLifetimeSeconds or > MaximumChallengeLifetimeSeconds)
                throw new ArgumentOutOfRangeException(nameof(challengeLifetimeSeconds));

            var origins = new HashSet<string>(StringComparer.Ordinal);
            foreach (var origin in allowedOrigins)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(origin);
                var normalized = NormalizeOrigin(origin);
                var uri = new Uri(normalized, UriKind.Absolute);
                var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();

                if (!HostMatchesRelyingParty(host, rpId))
                    throw new ArgumentException("Each WebAuthn origin host must equal or be a subdomain of the relying-party identifier.", nameof(allowedOrigins));

                origins.Add(normalized);
            }

            if (origins.Count == 0)
                throw new ArgumentException("At least one WebAuthn origin is required.", nameof(allowedOrigins));

            RelyingPartyId = rpId;
            RelyingPartyName = relyingPartyName.Trim();
            AllowedOrigins = origins;
            ChallengeLifetimeSeconds = challengeLifetimeSeconds;
        }

        private static string NormalizeOrigin(string value)
        {
            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
                string.IsNullOrEmpty(uri.Host) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                (uri.AbsolutePath.Length > 0 && uri.AbsolutePath != "/"))
            {
                throw new ArgumentException("WebAuthn origins must be absolute origins without path, query, fragment, or user information.", nameof(value));
            }

            var isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            var isLocalHttp =
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                 System.Net.IPAddress.TryParse(uri.Host, out var address) &&
                 address is not null &&
                 System.Net.IPAddress.IsLoopback(address));

            if (!isHttps && !isLocalHttp)
                throw new ArgumentException("WebAuthn origins must use HTTPS except for localhost or loopback development origins.", nameof(value));

            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        private static bool HostMatchesRelyingParty(string host, string relyingPartyId) =>
            string.Equals(host, relyingPartyId, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith('.' + relyingPartyId, StringComparison.OrdinalIgnoreCase);
    }
}
