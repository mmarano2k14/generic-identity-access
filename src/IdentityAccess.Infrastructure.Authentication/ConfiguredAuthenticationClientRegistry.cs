using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;
using Microsoft.Extensions.Configuration;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>
    /// Provides the immutable server-side authentication-client registry loaded from trusted
    /// configuration.
    /// </summary>
    internal sealed class ConfiguredAuthenticationClientRegistry :
        IAuthenticationClientRegistry
    {
        private readonly IReadOnlyDictionary<string, AuthenticationClientRegistration> clients;

        /// <summary>Initializes a new immutable registry from validated client registrations.</summary>
        public ConfiguredAuthenticationClientRegistry(
            IEnumerable<AuthenticationClientRegistration> clients)
        {
            ArgumentNullException.ThrowIfNull(clients);

            var dictionary =
                new Dictionary<string, AuthenticationClientRegistration>(
                    StringComparer.Ordinal);

            foreach (var client in clients)
            {
                ArgumentNullException.ThrowIfNull(client);

                if (!dictionary.TryAdd(client.ClientId, client))
                {
                    throw new InvalidOperationException(
                        "Duplicate authentication client registration.");
                }
            }

            if (dictionary.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one authentication client must be registered.");
            }

            this.clients = dictionary;
        }

        /// <inheritdoc />
        public bool TryGet(
            string clientId,
            out AuthenticationClientRegistration registration)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                registration = null!;
                return false;
            }

            return clients.TryGetValue(clientId, out registration!);
        }

        /// <summary>
        /// Loads and validates the authentication-client registry from trusted configuration.
        /// </summary>
        internal static ConfiguredAuthenticationClientRegistry FromConfiguration(
            IConfigurationSection clientsSection)
        {
            ArgumentNullException.ThrowIfNull(clientsSection);

            var registrations = new List<AuthenticationClientRegistration>();

            foreach (var section in clientsSection.GetChildren())
            {
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ClientId",
                    "ApplicationKey",
                    "AuthenticationContextKey",
                    "RedirectUris",
                    "PostLogoutRedirectUris",
                    "OidcEnabled",
                    "AllowedOidcScopes"
                };

                if (section.GetChildren().Any(child => !allowed.Contains(child.Key)))
                {
                    throw new InvalidOperationException(
                        "Authentication client configuration contains an unsupported field.");
                }

                var clientId = section["ClientId"] ??
                    throw new InvalidOperationException(
                        "Authentication client id is required.");

                var application = new ApplicationKey(
                    section["ApplicationKey"] ??
                    throw new InvalidOperationException(
                        "Authentication application key is required."));

                var contextKey = section["AuthenticationContextKey"] ??
                    throw new InvalidOperationException(
                        "Authentication context key is required.");

                var oidcEnabled =
                    ReadBoolean(
                        section,
                        "OidcEnabled",
                        fallback: false);

                registrations.Add(
                    new AuthenticationClientRegistration(
                        clientId,
                        application,
                        contextKey,
                        ReadArray(section.GetSection("RedirectUris")),
                        ReadArray(section.GetSection("PostLogoutRedirectUris")),
                        oidcEnabled,
                        ReadArray(section.GetSection("AllowedOidcScopes"))));
            }

            return new ConfiguredAuthenticationClientRegistry(registrations);
        }

        internal bool HasOidcClients =>
            clients.Values.Any(client => client.OidcEnabled);

        private static bool ReadBoolean(
            IConfigurationSection section,
            string key,
            bool fallback)
        {
            var raw = section[key];

            if (raw is null)
                return fallback;

            if (string.Equals(raw, "true", StringComparison.Ordinal))
                return true;

            if (string.Equals(raw, "false", StringComparison.Ordinal))
                return false;

            throw new InvalidOperationException(
                $"Authentication client option '{key}' must be true or false.");
        }

        private static string[] ReadArray(IConfigurationSection section) =>
            section.GetChildren()
                .Select(
                    child => child.Value ??
                        throw new InvalidOperationException(
                            "Authentication URI entries must be strings."))
                .ToArray();
    }
}
