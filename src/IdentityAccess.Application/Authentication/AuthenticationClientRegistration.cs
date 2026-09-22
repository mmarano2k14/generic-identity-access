using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>
    /// Server-registered login client. A client identifier or redirect URI supplied by a caller is a request,
    /// never proof of trust. Redirect matching is exact and registration-time validated.
    /// </summary>
    public sealed class AuthenticationClientRegistration
    {
        private readonly HashSet<string> _redirectUris;
        private readonly HashSet<string> _postLogoutRedirectUris;
        private readonly HashSet<string> _allowedOidcScopes;

        /// <summary>Gets the client identifier.</summary>
        public string ClientId { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the authentication context key.</summary>
        public string AuthenticationContextKey { get; }
        /// <summary>Gets the redirect URIs.</summary>
        public IReadOnlyCollection<string> RedirectUris => _redirectUris;
        /// <summary>Gets the post logout redirect URIs.</summary>
        public IReadOnlyCollection<string> PostLogoutRedirectUris => _postLogoutRedirectUris;

        /// <summary>Gets whether this registered client may use the OIDC authorization-code flow.</summary>
        public bool OidcEnabled { get; }

        /// <summary>Gets the OIDC scopes allowed for this public client.</summary>
        public IReadOnlyCollection<string> AllowedOidcScopes => _allowedOidcScopes;

        /// <summary>Initializes a new instance of <see cref="AuthenticationClientRegistration"/>.</summary>
        public AuthenticationClientRegistration(
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            IEnumerable<string> redirectUris,
            IEnumerable<string>? postLogoutRedirectUris = null,
            bool oidcEnabled = false,
            IEnumerable<string>? allowedOidcScopes = null)
        {
            ClientId = ValidateClientId(clientId);
            Application = application ?? throw new ArgumentNullException(nameof(application));
            AuthenticationContextKey = ValidateContextKey(authenticationContextKey);
            _redirectUris = ValidateUris(redirectUris, requireAtLeastOne: true, nameof(redirectUris));
            _postLogoutRedirectUris = ValidateUris(
                postLogoutRedirectUris ?? [],
                requireAtLeastOne: false,
                nameof(postLogoutRedirectUris));

            OidcEnabled = oidcEnabled;
            _allowedOidcScopes = ValidateOidcScopes(
                allowedOidcScopes ?? [],
                oidcEnabled,
                nameof(allowedOidcScopes));
        }

        /// <summary>Determines whether the URI is an exact registered login redirect URI.</summary>
        public bool AllowsRedirectUri(string uri) =>
            !string.IsNullOrWhiteSpace(uri) && _redirectUris.Contains(uri);

        /// <summary>Determines whether the URI is an exact registered post-logout redirect URI.</summary>
        public bool AllowsPostLogoutRedirectUri(string uri) =>
            !string.IsNullOrWhiteSpace(uri) && _postLogoutRedirectUris.Contains(uri);

        /// <summary>Determines whether the canonical OIDC scope is registered for the client.</summary>
        public bool AllowsOidcScope(string scope) =>
            OidcEnabled &&
            !string.IsNullOrWhiteSpace(scope) &&
            _allowedOidcScopes.Contains(scope);

        private static string ValidateClientId(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (value.Length > 128 || value[0] is < 'a' or > 'z' ||
                value.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')))
                throw new ArgumentException(
                    "Client identifiers use 1 to 128 lowercase letters, digits, hyphens or underscores, starting with a letter.",
                    nameof(value));
            return value;
        }

        private static string ValidateContextKey(string value)
        {
            try
            {
                return new ApplicationKey(value).Value;
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    "Authentication context keys must use the application-key grammar.",
                    nameof(value),
                    exception);
            }
        }

        private static HashSet<string> ValidateOidcScopes(
            IEnumerable<string> values,
            bool oidcEnabled,
            string parameter)
        {
            ArgumentNullException.ThrowIfNull(values);

            var result = new HashSet<string>(StringComparer.Ordinal);

            foreach (var value in values)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);

                if (!string.Equals(value, "openid", StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "This release supports only the 'openid' OIDC scope.",
                        parameter);
                }

                if (!result.Add(value))
                {
                    throw new ArgumentException(
                        "Duplicate OIDC scopes are not allowed.",
                        parameter);
                }
            }

            if (oidcEnabled && !result.Contains("openid"))
            {
                throw new ArgumentException(
                    "OIDC-enabled clients must register the 'openid' scope.",
                    parameter);
            }

            if (!oidcEnabled && result.Count != 0)
            {
                throw new ArgumentException(
                    "OIDC scopes cannot be registered when OIDC is disabled for the client.",
                    parameter);
            }

            return result;
        }

        private static HashSet<string> ValidateUris(IEnumerable<string> values, bool requireAtLeastOne, string parameter)
        {
            ArgumentNullException.ThrowIfNull(values);
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
                if (value.Contains('*', StringComparison.Ordinal) ||
                    !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                    !string.IsNullOrEmpty(uri.Fragment) ||
                    !string.IsNullOrEmpty(uri.UserInfo) ||
                    (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
                    throw new ArgumentException(
                        "Redirect URIs must be exact absolute HTTPS URIs, or HTTP loopback URIs for local development, without fragments, userinfo or wildcards.",
                        parameter);

                if (!result.Add(value))
                    throw new ArgumentException("Duplicate redirect URIs are not allowed.", parameter);
                if (result.Count > 32)
                    throw new ArgumentException("At most 32 redirect URIs may be registered.", parameter);
            }

            if (requireAtLeastOne && result.Count == 0)
                throw new ArgumentException("At least one redirect URI is required.", parameter);
            return result;
        }
    }
}
