using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Registers the recovery-code authentication-factor provider from trusted server configuration.</summary>
    public static class RecoveryServiceCollectionExtensions
    {
        /// <summary>Adds recovery codes only when the provider is explicitly enabled.</summary>
        public static IServiceCollection AddIdentityAccessRecoveryProvider(
            this IServiceCollection services,
            IConfigurationSection section)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(section);

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Enabled",
                "CodeCount"
            };

            if (section.GetChildren().Any(child => !allowed.Contains(child.Key)))
                throw new InvalidOperationException("Recovery-code configuration contains an unsupported field.");

            var enabled = section["Enabled"];
            if (enabled is null or "false")
            {
                if (section.GetChildren().Any(child => !string.Equals(child.Key, "Enabled", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Recovery-code options cannot be configured while the provider is disabled.");
                return services;
            }

            if (!string.Equals(enabled, "true", StringComparison.Ordinal))
                throw new InvalidOperationException("IdentityAccess:Mfa:Recovery:Enabled must be true or false.");

            RequireService<IDatabaseRouteResolver>(services);
            RequireService<IIdentityDatabaseConnectionFactory>(services);
            RequireService<ISecurityAuditWriter>(services);
            RequireService<IAuthenticationFactorProviderRegistry>(services);
            RequireService<IMfaProviderPolicyGuard>(services);
            RequireService<TimeProvider>(services);

            var codeCount = ReadInt(section, "CodeCount", RecoveryCodeProviderOptions.DefaultCodeCount);
            var options = new RecoveryCodeProviderOptions(codeCount);

            services.AddSingleton(options);
            services.AddSingleton<IRecoveryCodeStore, PostgreSqlRecoveryCodeStore>();
            services.AddSingleton<IRecoveryAuthenticationFactorService, RecoveryAuthenticationFactorService>();
            services.AddIdentityAccessAuthenticationFactorProvider<RecoveryAuthenticationFactorProvider>();
            return services;
        }

        private static int ReadInt(IConfigurationSection section, string key, int fallback)
        {
            var value = section[key];
            if (value is null) return fallback;
            if (!int.TryParse(value, out var parsed))
                throw new InvalidOperationException($"Recovery-code option '{key}' must be an integer.");
            return parsed;
        }

        private static void RequireService<TService>(IServiceCollection services)
        {
            if (!services.Any(descriptor => descriptor.ServiceType == typeof(TService)))
                throw new InvalidOperationException($"Recovery codes require server service '{typeof(TService).Name}'.");
        }
    }
}
