using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Registers the WebAuthn provider from trusted server configuration.</summary>
    public static class WebAuthnServiceCollectionExtensions
    {
        /// <summary>Adds WebAuthn registration and assertion verification only when the provider is explicitly enabled.</summary>
        public static IServiceCollection AddIdentityAccessWebAuthnProvider(
            this IServiceCollection services,
            IConfigurationSection section)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(section);

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Enabled",
                "RelyingPartyId",
                "RelyingPartyName",
                "AllowedOrigins",
                "ChallengeLifetimeSeconds"
            };

            if (section.GetChildren().Any(child => !allowed.Contains(child.Key)))
                throw new InvalidOperationException("WebAuthn configuration contains an unsupported field.");

            var enabled = section["Enabled"];
            if (enabled is null or "false")
            {
                if (section.GetChildren().Any(child => !string.Equals(child.Key, "Enabled", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("WebAuthn options cannot be configured while the provider is disabled.");
                return services;
            }

            if (!string.Equals(enabled, "true", StringComparison.Ordinal))
                throw new InvalidOperationException("IdentityAccess:Mfa:WebAuthn:Enabled must be true or false.");

            RequireService<IDatabaseRouteResolver>(services);
            RequireService<IIdentityDatabaseConnectionFactory>(services);
            RequireService<ISecurityAuditWriter>(services);
            RequireService<IAuthenticationFactorProviderRegistry>(services);
            RequireService<IMfaProviderPolicyGuard>(services);
            RequireService<TimeProvider>(services);

            var rpId = section["RelyingPartyId"];
            var rpName = section["RelyingPartyName"];
            if (string.IsNullOrWhiteSpace(rpId))
                throw new InvalidOperationException("IdentityAccess:Mfa:WebAuthn:RelyingPartyId is required when WebAuthn is enabled.");
            if (string.IsNullOrWhiteSpace(rpName))
                throw new InvalidOperationException("IdentityAccess:Mfa:WebAuthn:RelyingPartyName is required when WebAuthn is enabled.");

            var origins = section.GetSection("AllowedOrigins").GetChildren().Select(child => child.Value).Where(value => value is not null).Cast<string>().ToArray();
            if (origins.Length == 0)
                throw new InvalidOperationException("IdentityAccess:Mfa:WebAuthn:AllowedOrigins requires at least one origin when WebAuthn is enabled.");

            var lifetime = ReadInt(
                section,
                "ChallengeLifetimeSeconds",
                WebAuthnProviderOptions.DefaultChallengeLifetimeSeconds);

            var options = new WebAuthnProviderOptions(rpId, rpName, origins, lifetime);
            services.AddSingleton(options);
            services.AddSingleton<IWebAuthnCredentialStore, PostgreSqlWebAuthnCredentialStore>();
            services.AddSingleton<IWebAuthnRegistrationService, WebAuthnRegistrationService>();
            services.AddSingleton<IWebAuthnAuthenticationService, WebAuthnAuthenticationService>();
            services.AddIdentityAccessAuthenticationFactorProvider<WebAuthnAuthenticationFactorProvider>();
            return services;
        }

        private static int ReadInt(IConfigurationSection section, string key, int fallback)
        {
            var value = section[key];
            if (value is null) return fallback;
            if (!int.TryParse(value, out var parsed))
                throw new InvalidOperationException($"WebAuthn option '{key}' must be an integer.");
            return parsed;
        }

        private static void RequireService<TService>(IServiceCollection services)
        {
            if (!services.Any(descriptor => descriptor.ServiceType == typeof(TService)))
                throw new InvalidOperationException($"WebAuthn requires server service '{typeof(TService).Name}'.");
        }
    }
}
