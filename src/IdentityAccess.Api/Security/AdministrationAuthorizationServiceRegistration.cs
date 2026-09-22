using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Rbac;
using IdentityAccess.Rbac.MultiplexedAdapter;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Registers server-side administration authorization orchestration and the configured
    /// external RBAC adapter.
    /// </summary>
    internal static class AdministrationAuthorizationServiceRegistration
    {
        /// <summary>
        /// Registers real administration capability authorization when explicitly enabled.
        /// </summary>
        public static void AddIdentityAdministrationAuthorization(
            this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var section = builder.Configuration.GetSection(
                "IdentityAccess:Authorization");

            var allowed = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "Enabled",
                "Provider",
                "RbacProject",
                "RbacNamespace",
                "ReferenceDirectory"
            };

            if (section.GetChildren().Any(
                child => !allowed.Contains(child.Key)))
            {
                throw new InvalidOperationException(
                    "Authorization configuration contains an unsupported field.");
            }

            var enabled = section["Enabled"];

            if (enabled is null or "false")
            {
                if (section.GetChildren().Any(
                    child => !string.Equals(
                        child.Key,
                        "Enabled",
                        StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "Authorization options cannot be configured while authorization is disabled.");
                }

                return;
            }

            if (!string.Equals(
                    enabled,
                    "true",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "IdentityAccess:Authorization:Enabled must be true or false.");
            }

            RequireService<IDatabaseRouteResolver>(builder.Services);
            RequireService<IAssignedCapabilityReader>(builder.Services);
            RequireService<IIdentityScopeAssignedCapabilityReader>(builder.Services);

            var provider = Require(
                section,
                "Provider");

            if (!string.Equals(
                    provider,
                    "multiplexed",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The configured authorization provider is unsupported.");
            }

            var options = new AdministrationAuthorizationOptions(
                Require(section, "RbacProject"),
                Require(section, "RbacNamespace"));

            var configuredReferenceDirectory = Require(
                section,
                "ReferenceDirectory");

            string referenceDirectory;

            try
            {
                referenceDirectory = Path.GetFullPath(
                    configuredReferenceDirectory,
                    builder.Environment.ContentRootPath);
            }
            catch (Exception error) when (
                error is ArgumentException or
                    NotSupportedException or
                    System.Security.SecurityException)
            {
                throw new InvalidOperationException(
                    "The authorization reference directory is invalid.",
                    error);
            }

            var adapterOptions =
                new MultiplexedRbacAdapterOptions(
                    referenceDirectory);

            var adapter =
                new MultiplexedRbacAuthorizationAdapter(
                    adapterOptions);

            var compatibility =
                adapter.ProbeCompatibility();

            if (!compatibility.IsCompatible)
            {
                throw new InvalidOperationException(
                    $"External RBAC compatibility preflight failed ({compatibility.FailureCode}): {compatibility.DiagnosticDetail}");
            }

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton(adapterOptions);
            builder.Services.AddSingleton(compatibility);
            builder.Services.AddSingleton<IMultiplexedRbacCompatibilityProbe>(adapter);
            builder.Services.AddSingleton<IRbacAuthorizationAdapter>(adapter);
            builder.Services.AddSingleton<RbacTrnCompiler>();
            builder.Services.AddSingleton<
                IIdentityAuthorizationService,
                IdentityAuthorizationService>();
            builder.Services.AddSingleton<
                IIdentityScopeAuthorizationService,
                IdentityScopeAuthorizationService>();
        }

        private static string Require(
            IConfigurationSection section,
            string key)
        {
            var value = section[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Authorization configuration '{key}' is required.");
            }

            return value;
        }


        private static void RequireService<TService>(
            IServiceCollection services)
        {
            if (!services.Any(
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(TService)))
            {
                throw new InvalidOperationException(
                    $"Administration authorization requires server service '{typeof(TService).Name}'.");
            }
        }
    }
}
