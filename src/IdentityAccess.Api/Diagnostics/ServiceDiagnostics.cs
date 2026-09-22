using System.Reflection;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Contracts;

namespace IdentityAccess.Api.Diagnostics
{
    /// <summary>
    /// Derives safe service metadata and readiness state from the actual host service graph.
    /// </summary>
    internal sealed class ServiceDiagnostics(
        OptionalFeature<IDatabaseRouteResolver> routing,
        OptionalFeature<IIdentityDatabaseConnectionFactory> storage,
        OptionalFeature<ILocalAuthenticationService> authentication,
        IAdministrationRequestAuthorizer administrationAuthorizer) : IServiceDiagnostics
    {
        private static readonly string ModuleVersion = ResolveModuleVersion();

        /// <inheritdoc />
        public ServiceInfoResponse Describe()
        {
            var state = Snapshot();

            return new ServiceInfoResponse(
                Service: "identity-access",
                ApiVersion: "v1",
                ModuleVersion: ModuleVersion,
                Stage: state.Ready ? "operational" : "configuration",
                StorageProvider: "postgresql",
                DatabaseRoutingConfigured: state.DatabaseRoutingConfigured,
                StorageConfigured: state.StorageConfigured,
                AuthenticationConfigured: state.AuthenticationConfigured,
                AuthorizationConfigured: state.AuthorizationConfigured);
        }

        /// <inheritdoc />
        public ReadinessResponse Readiness()
        {
            var state = Snapshot();

            return new ReadinessResponse(
                state.Ready,
                state.Ready ? "operational" : "configuration",
                state.BlockingCapabilities);
        }

        private ServiceRuntimeState Snapshot()
        {
            var routingConfigured = routing.IsAvailable;
            var storageConfigured = storage.IsAvailable;
            var authenticationConfigured = authentication.IsAvailable;
            var authorizationConfigured =
                administrationAuthorizer.CapabilityAuthorizationAvailable;

            var blockers = new List<string>();

            if (!routingConfigured)
            {
                blockers.Add("database-routing");
            }

            if (!storageConfigured)
            {
                blockers.Add("postgresql-persistence");
            }

            if (!authenticationConfigured)
            {
                blockers.Add("authentication");
            }

            if (!authorizationConfigured)
            {
                blockers.Add("administration-authorization");
            }

            return new ServiceRuntimeState(
                routingConfigured,
                storageConfigured,
                authenticationConfigured,
                authorizationConfigured,
                blockers.Count == 0,
                blockers.AsReadOnly());
        }

        private static string ResolveModuleVersion()
        {
            var assembly = typeof(ServiceDiagnostics).Assembly;
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
            {
                var metadataSeparator = informational.IndexOf('+');
                return metadataSeparator >= 0
                    ? informational[..metadataSeparator]
                    : informational;
            }

            return assembly.GetName().Version?.ToString() ?? "unknown";
        }
    }
}
