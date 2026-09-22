using System.Net.Http.Json;
using System.Reflection;
using IdentityAccess.Contracts;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects runtime diagnostics from regressing to stale hardcoded foundation metadata.
    /// </summary>
    public sealed class RuntimeDiagnosticsArchitectureTests
    {
        /// <summary>
        /// Verifies that the obsolete application-layer FoundationStatus type is absent.
        /// </summary>
        [Fact]
        public void Application_assembly_does_not_contain_foundation_status()
        {
            var applicationAssembly = typeof(IdentityAccess.Application.Routing.IDatabaseRouteResolver).Assembly;

            Assert.DoesNotContain(
                applicationAssembly.GetTypes(),
                type => string.Equals(
                    type.Name,
                    "FoundationStatus",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifies that the system information endpoint reports the assembly informational
        /// version rather than a stale hardcoded module version.
        /// </summary>
        [Fact]
        public async Task System_info_uses_running_api_assembly_version()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var info = await client.GetFromJsonAsync<ServiceInfoResponse>(
                "/api/v1/system/info",
                TestContext.Current.CancellationToken);

            Assert.NotNull(info);

            var informational = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion;
            var separator = informational.IndexOf('+');
            var expected = separator >= 0
                ? informational[..separator]
                : informational;

            Assert.Equal(expected, info.ModuleVersion);
        }

        /// <summary>
        /// Verifies that the API registers tagged ASP.NET Core health checks for liveness and
        /// readiness rather than implementing readiness only as controller-local metadata.
        /// </summary>
        [Fact]
        public void Program_registers_tagged_health_checks()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(
                Path.Combine(root, "src", "IdentityAccess.Api", "Diagnostics", "DiagnosticsServiceRegistration.cs"));

            Assert.Contains("tags: [\"live\"]", source, StringComparison.Ordinal);
            Assert.Contains("tags: [\"ready\"]", source, StringComparison.Ordinal);
            Assert.Contains("AddCheck<IdentityAccessReadinessHealthCheck>", source, StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
