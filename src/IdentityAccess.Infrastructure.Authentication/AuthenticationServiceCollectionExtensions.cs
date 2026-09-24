using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>
    /// Registers local authentication and optional OAuth 2.0 / OpenID Connect infrastructure from
    /// trusted server configuration.
    /// </summary>
    public static class AuthenticationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds local authentication using the process base directory for relative infrastructure
        /// paths. Existing callers may keep this overload; hosts enabling OIDC should prefer the
        /// overload that supplies an explicit content root.
        /// </summary>
        /// <param name="services">The server service collection.</param>
        /// <param name="section">The <c>IdentityAccess:Authentication</c> configuration section.</param>
        /// <returns>The original service collection.</returns>
        public static IServiceCollection AddIdentityAccessAuthentication(
            this IServiceCollection services,
            IConfigurationSection section) =>
            AddIdentityAccessAuthentication(
                services,
                section,
                AppContext.BaseDirectory);

        /// <summary>
        /// Adds one pluggable authentication-factor provider to the generic provider registry.
        /// </summary>
        /// <typeparam name="TProvider">The provider implementation type.</typeparam>
        /// <param name="services">The server service collection.</param>
        /// <returns>The original service collection.</returns>
        public static IServiceCollection AddIdentityAccessAuthenticationFactorProvider<TProvider>(
            this IServiceCollection services)
            where TProvider : class, IAuthenticationFactorProvider
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IAuthenticationFactorProvider, TProvider>();
            return services;
        }

        /// <summary>
        /// Adds local authentication and optional OIDC services when explicitly enabled.
        /// </summary>
        /// <param name="services">The server service collection.</param>
        /// <param name="section">The <c>IdentityAccess:Authentication</c> configuration section.</param>
        /// <param name="contentRootPath">The trusted server content root used for signing-key paths.</param>
        /// <returns>The original service collection.</returns>
        public static IServiceCollection AddIdentityAccessAuthentication(
            this IServiceCollection services,
            IConfigurationSection section,
            string contentRootPath)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(section);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

            var enabled = section["Enabled"];

            var allowed =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "Enabled",
                    "SessionLifetimeMinutes",
                    "LockoutAttempts",
                    "LockoutMinutes",
                    "Clients",
                    "Oidc"
                };

            if (section.GetChildren().Any(
                    child => !allowed.Contains(child.Key)))
            {
                throw new InvalidOperationException(
                    "Authentication configuration contains an unsupported field.");
            }

            if (enabled is null or "false")
            {
                if (section.GetChildren().Any(
                        child => !string.Equals(
                            child.Key,
                            "Enabled",
                            StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "Authentication options cannot be configured while authentication is disabled.");
                }

                return services;
            }

            if (!string.Equals(
                    enabled,
                    "true",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "IdentityAccess:Authentication:Enabled must be true or false.");
            }

            RequireService<IAuthenticationDirectoryLocator>(services);
            RequireService<IPasswordCredentialStore>(services);
            RequireService<ICredentialMutationStore>(services);
            RequireService<IAuthenticationSessionStore>(services);
            RequireService<ISecurityAuditWriter>(services);

            var options =
                new AuthenticationOptions
                {
                    SessionLifetimeMinutes = ReadInt(
                        section,
                        "SessionLifetimeMinutes",
                        AuthenticationOptions.DefaultSessionLifetimeMinutes),

                    LockoutAttempts = ReadInt(
                        section,
                        "LockoutAttempts",
                        AuthenticationOptions.DefaultLockoutAttempts),

                    LockoutMinutes = ReadInt(
                        section,
                        "LockoutMinutes",
                        AuthenticationOptions.DefaultLockoutMinutes)
                };

            options.Validate();

            var registry =
                ConfiguredAuthenticationClientRegistry.FromConfiguration(
                    section.GetSection("Clients"));

            services.AddSingleton(options);
            services.AddSingleton<IAuthenticationClientRegistry>(registry);
            services.AddSingleton<IPasswordHashingService, AspNetCorePasswordHashingService>();
            services.AddSingleton<ISessionTokenService, CryptographicSessionTokenService>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ILocalAuthenticationService, LocalAuthenticationService>();
            services.AddSingleton<ICredentialAdministrationService, CredentialAdministrationService>();
            services.AddSingleton<ISessionAdministrationService, SessionAdministrationService>();
            AddMfaAdministration(services);

            AddOidc(
                services,
                section.GetSection("Oidc"),
                contentRootPath,
                registry);

            return services;
        }

        private static void AddOidc(
            IServiceCollection services,
            IConfigurationSection section,
            string contentRootPath,
            ConfiguredAuthenticationClientRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(section);

            var allowed =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "Enabled",
                    "Issuer",
                    "AccessTokenAudience",
                    "AuthorizationCodeLifetimeSeconds",
                    "AccessTokenLifetimeMinutes",
                    "IdTokenLifetimeMinutes",
                    "RefreshTokenLifetimeDays",
                    "ActiveSigningKeyId",
                    "SigningKeys",
                    "SigningKeyId",
                    "SigningKeyPemPath"
                };

            if (section.GetChildren().Any(
                    child => !allowed.Contains(child.Key)))
            {
                throw new InvalidOperationException(
                    "OIDC configuration contains an unsupported field.");
            }

            var enabled =
                section["Enabled"];

            if (enabled is null or "false")
            {
                if (section.GetChildren().Any(
                        child => !string.Equals(
                            child.Key,
                            "Enabled",
                            StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "OIDC options cannot be configured while OIDC is disabled.");
                }

                return;
            }

            if (!string.Equals(
                    enabled,
                    "true",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "IdentityAccess:Authentication:Oidc:Enabled must be true or false.");
            }

            if (!registry.HasOidcClients)
            {
                throw new InvalidOperationException(
                    "OIDC requires at least one OIDC-enabled authentication client.");
            }

            RequireService<IOidcAuthorizationCodeStore>(services);
            RequireService<IOidcRefreshTokenStore>(services);

            var oidcOptions =
                new OidcOptions
                {
                    Issuer = Require(
                        section,
                        "Issuer"),

                    AccessTokenAudience =
                        section["AccessTokenAudience"] ??
                        "identity-access-api",

                    AuthorizationCodeLifetimeSeconds =
                        ReadInt(
                            section,
                            "AuthorizationCodeLifetimeSeconds",
                            OidcOptions.DefaultAuthorizationCodeLifetimeSeconds),

                    AccessTokenLifetimeMinutes =
                        ReadInt(
                            section,
                            "AccessTokenLifetimeMinutes",
                            OidcOptions.DefaultAccessTokenLifetimeMinutes),

                    IdTokenLifetimeMinutes =
                        ReadInt(
                            section,
                            "IdTokenLifetimeMinutes",
                            OidcOptions.DefaultIdTokenLifetimeMinutes),

                    RefreshTokenLifetimeDays =
                        ReadInt(
                            section,
                            "RefreshTokenLifetimeDays",
                            OidcOptions.DefaultRefreshTokenLifetimeDays)
                };

            oidcOptions.Validate();

            var (activeSigningKeyId, signingKeys) =
                RsaOidcSigningKeyConfigurationLoader.Load(
                    section,
                    contentRootPath);

            var tokenIssuer =
                new RsaOidcTokenIssuer(
                    oidcOptions,
                    activeSigningKeyId,
                    signingKeys);

            _ = tokenIssuer.SigningKeys;

            services.AddSingleton(oidcOptions);
            services.AddSingleton<IOidcCodeService, CryptographicOidcCodeService>();
            services.AddSingleton<IOidcRefreshTokenService, CryptographicOidcRefreshTokenService>();
            services.AddSingleton<IOidcTokenIssuer>(_ => tokenIssuer);
            services.AddSingleton<IOidcAccessTokenValidator>(provider =>
                new RsaOidcAccessTokenValidator(
                    oidcOptions,
                    registry,
                    provider.GetRequiredService<IOidcTokenIssuer>(),
                    provider.GetRequiredService<TimeProvider>()));
            services.AddSingleton<IOidcAccessTokenSessionValidator, OidcAccessTokenSessionValidator>();
            services.AddSingleton<IOidcAuthorizationService, OidcAuthorizationService>();
        }

        private static string Require(
            IConfigurationSection section,
            string key)
        {
            var value =
                section[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"OIDC configuration '{key}' is required.");
            }

            return value;
        }

        private static int ReadInt(
            IConfigurationSection section,
            string key,
            int fallback)
        {
            var value =
                section[key];

            if (value is null)
            {
                return fallback;
            }

            if (!int.TryParse(
                    value,
                    out var parsed))
            {
                throw new InvalidOperationException(
                    $"Authentication option '{key}' must be an integer.");
            }

            return parsed;
        }


        private static void AddMfaAdministration(IServiceCollection services)
        {
            services.AddIdentityAccessAuthenticationFactorProviderRegistry();

            var hasPolicyStore = HasService<IMfaPolicyStore>(services);
            var hasAuthenticatorStore = HasService<IUserAuthenticatorStore>(services);

            if (hasPolicyStore != hasAuthenticatorStore)
            {
                throw new InvalidOperationException(
                    "MFA administration requires both generic MFA persistence stores when either is registered.");
            }

            if (hasPolicyStore)
            {
                services.AddSingleton<IMfaProviderPolicyGuard, MfaProviderPolicyGuard>();
                services.AddSingleton<IMfaAdministrationService, MfaAdministrationService>();
            }
        }

        private static bool HasService<TService>(IServiceCollection services) =>
            services.Any(descriptor => descriptor.ServiceType == typeof(TService));

        private static void RequireService<TService>(
            IServiceCollection services)
        {
            if (!services.Any(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(TService)))
            {
                throw new InvalidOperationException(
                    $"Authentication requires server service '{typeof(TService).Name}'.");
            }
        }
    }
}
