namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines the enabled OpenID Connect authorization-code and refresh-token provider configuration.</summary>
    public sealed class OidcOptions
    {
        /// <summary>Defines the default authorization-code lifetime in seconds.</summary>
        public const int DefaultAuthorizationCodeLifetimeSeconds = 120;

        /// <summary>Defines the default access-token lifetime in minutes.</summary>
        public const int DefaultAccessTokenLifetimeMinutes = 15;

        /// <summary>Defines the default ID-token lifetime in minutes.</summary>
        public const int DefaultIdTokenLifetimeMinutes = 15;

        /// <summary>Defines the default absolute refresh-token family lifetime in days.</summary>
        public const int DefaultRefreshTokenLifetimeDays = 30;

        /// <summary>Defines the default maximum age of MFA assurance accepted by OIDC authorization.</summary>
        public const int DefaultMfaMaxAgeMinutes = 15;

        /// <summary>Gets or initializes the canonical provider issuer.</summary>
        public string Issuer { get; init; } = string.Empty;

        /// <summary>Gets or initializes the access-token audience.</summary>
        public string AccessTokenAudience { get; init; } = "identity-access-api";

        /// <summary>Gets or initializes the authorization-code lifetime in seconds.</summary>
        public int AuthorizationCodeLifetimeSeconds { get; init; } =
            DefaultAuthorizationCodeLifetimeSeconds;

        /// <summary>Gets or initializes the access-token lifetime in minutes.</summary>
        public int AccessTokenLifetimeMinutes { get; init; } =
            DefaultAccessTokenLifetimeMinutes;

        /// <summary>Gets or initializes the ID-token lifetime in minutes.</summary>
        public int IdTokenLifetimeMinutes { get; init; } =
            DefaultIdTokenLifetimeMinutes;

        /// <summary>Gets or initializes the absolute refresh-token family lifetime in days.</summary>
        public int RefreshTokenLifetimeDays { get; init; } =
            DefaultRefreshTokenLifetimeDays;

        /// <summary>Gets or initializes the maximum accepted age of required MFA assurance.</summary>
        public int MfaMaxAgeMinutes { get; init; } =
            DefaultMfaMaxAgeMinutes;

        /// <summary>Validates the current value and normalizes the issuer.</summary>
        public OidcOptions Validate()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
            ArgumentException.ThrowIfNullOrWhiteSpace(AccessTokenAudience);

            if (!Uri.TryCreate(Issuer, UriKind.Absolute, out var issuer) ||
                !string.IsNullOrEmpty(issuer.Query) ||
                !string.IsNullOrEmpty(issuer.Fragment) ||
                !string.IsNullOrEmpty(issuer.UserInfo) ||
                (!string.IsNullOrEmpty(issuer.AbsolutePath) && issuer.AbsolutePath != "/") ||
                (issuer.Scheme != Uri.UriSchemeHttps &&
                    !(issuer.Scheme == Uri.UriSchemeHttp && issuer.IsLoopback)))
            {
                throw new ArgumentException(
                    "OIDC issuer must be an origin-only absolute HTTPS URI, or HTTP loopback URI for local development, without a path, query, fragment, or userinfo.",
                    nameof(Issuer));
            }

            if (AuthorizationCodeLifetimeSeconds is < 30 or > 600)
                throw new ArgumentOutOfRangeException(nameof(AuthorizationCodeLifetimeSeconds));

            if (AccessTokenLifetimeMinutes is < 5 or > 60)
                throw new ArgumentOutOfRangeException(nameof(AccessTokenLifetimeMinutes));

            if (IdTokenLifetimeMinutes is < 5 or > 60)
                throw new ArgumentOutOfRangeException(nameof(IdTokenLifetimeMinutes));

            if (RefreshTokenLifetimeDays is < 1 or > 365)
                throw new ArgumentOutOfRangeException(nameof(RefreshTokenLifetimeDays));

            if (MfaMaxAgeMinutes is < 1 or > 1440)
                throw new ArgumentOutOfRangeException(nameof(MfaMaxAgeMinutes));

            return this;
        }

        /// <summary>Returns the issuer without a trailing slash.</summary>
        public string CanonicalIssuer =>
            Issuer.TrimEnd('/');
    }
}
