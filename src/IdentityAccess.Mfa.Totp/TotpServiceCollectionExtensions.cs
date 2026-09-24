using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Registers the TOTP authentication-factor provider from trusted server configuration.</summary>
    public static class TotpServiceCollectionExtensions
    {
        /// <summary>Adds TOTP only when the provider is explicitly enabled.</summary>
        public static IServiceCollection AddIdentityAccessTotpProvider(
            this IServiceCollection services,
            IConfigurationSection section)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(section);

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Enabled",
                "Issuer",
                "AllowedClockSkewSteps"
            };

            if (section.GetChildren().Any(child => !allowed.Contains(child.Key)))
                throw new InvalidOperationException("TOTP configuration contains an unsupported field.");

            var enabled = section["Enabled"];
            if (enabled is null or "false")
            {
                if (section.GetChildren().Any(child => !string.Equals(child.Key, "Enabled", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("TOTP options cannot be configured while the provider is disabled.");
                return services;
            }

            if (!string.Equals(enabled, "true", StringComparison.Ordinal))
                throw new InvalidOperationException("IdentityAccess:Mfa:Totp:Enabled must be true or false.");

            RequireService<IDatabaseRouteResolver>(services);
            RequireService<IIdentityDatabaseConnectionFactory>(services);
            RequireService<ISecurityAuditWriter>(services);
            RequireService<IAuthenticationFactorProviderRegistry>(services);
            RequireService<IMfaProviderPolicyGuard>(services);
            RequireService<TimeProvider>(services);

            var issuer = section["Issuer"];
            if (string.IsNullOrWhiteSpace(issuer))
                throw new InvalidOperationException("IdentityAccess:Mfa:Totp:Issuer is required when TOTP is enabled.");

            var allowedClockSkewSteps = ReadInt(section, "AllowedClockSkewSteps", 1);
            var options = new TotpProviderOptions(issuer, allowedClockSkewSteps);

            services.AddDataProtection();
            services.AddSingleton(options);
            services.AddSingleton<ITotpSecretProtector, TotpSecretProtector>();
            services.AddSingleton<ITotpAuthenticatorStore, PostgreSqlTotpAuthenticatorStore>();
            services.AddSingleton<ITotpAuthenticationFactorService, TotpAuthenticationFactorService>();
            services.AddIdentityAccessAuthenticationFactorProvider<TotpAuthenticationFactorProvider>();
            return services;
        }

        private static int ReadInt(IConfigurationSection section, string key, int fallback)
        {
            var value = section[key];
            if (value is null) return fallback;
            if (!int.TryParse(value, out var parsed))
                throw new InvalidOperationException($"TOTP option '{key}' must be an integer.");
            return parsed;
        }

        private static void RequireService<TService>(IServiceCollection services)
        {
            if (!services.Any(descriptor => descriptor.ServiceType == typeof(TService)))
                throw new InvalidOperationException($"TOTP requires server service '{typeof(TService).Name}'.");
        }
    }
}
