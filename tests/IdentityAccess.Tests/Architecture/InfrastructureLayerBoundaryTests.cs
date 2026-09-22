using IdentityAccess.Infrastructure.Authentication;
using IdentityAccess.Infrastructure.ConfigurationRouting;
using IdentityAccess.Infrastructure.PostgreSql;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects infrastructure implementation details from leaking into the supported public
    /// API or back into the HTTP project.
    /// </summary>
    public sealed class InfrastructureLayerBoundaryTests
    {
        /// <summary>
        /// Verifies that authentication infrastructure exports only its registration surface.
        /// </summary>
        [Fact]
        public void Authentication_infrastructure_exports_only_registration_surface()
        {
            var exported = typeof(AuthenticationServiceCollectionExtensions)
                .Assembly
                .GetExportedTypes();

            Assert.Equal(
                [typeof(AuthenticationServiceCollectionExtensions)],
                exported.OrderBy(type => type.FullName).ToArray());
        }

        /// <summary>
        /// Verifies that PostgreSQL concrete implementations are not exported publicly.
        /// </summary>
        [Fact]
        public void PostgreSql_infrastructure_hides_concrete_implementations()
        {
            var exported = typeof(PostgreSqlServiceCollectionExtensions)
                .Assembly
                .GetExportedTypes()
                .Select(type => type.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    nameof(PostgreSqlServiceCollectionExtensions),
                    nameof(PostgreSqlStorageException),
                    nameof(PostgreSqlStorageFailure)
                }.OrderBy(name => name).ToArray(),
                exported);
        }

        /// <summary>
        /// Verifies that the file-based routing provider is internal while stable integration
        /// and configuration-failure contracts remain public.
        /// </summary>
        [Fact]
        public void Configuration_routing_hides_provider_implementation()
        {
            var exported = typeof(ConfigurationRoutingServiceCollectionExtensions)
                .Assembly
                .GetExportedTypes()
                .Select(type => type.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    nameof(ConfigurationRoutingServiceCollectionExtensions),
                    nameof(RoutingConfigurationException),
                    nameof(RoutingConfigurationFailure)
                }.OrderBy(name => name).ToArray(),
                exported);
        }

        /// <summary>
        /// Verifies that infrastructure implementations are no longer declared in the HTTP
        /// project assembly.
        /// </summary>
        [Theory]
        [InlineData("AspNetCorePasswordHashingService")]
        [InlineData("CryptographicSessionTokenService")]
        [InlineData("ConfiguredAuthenticationClientRegistry")]
        public void Api_assembly_does_not_contain_authentication_infrastructure(string typeName)
        {
            Assert.DoesNotContain(
                typeof(Program).Assembly.GetTypes(),
                type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifies that source namespaces under the API Security folder match the folder
        /// boundary.
        /// </summary>
        [Fact]
        public void Api_security_sources_use_security_namespace()
        {
            var root = FindRepositoryRoot();
            var securityFolder = Path.Combine(
                root,
                "src",
                "IdentityAccess.Api",
                "Security");

            foreach (var file in Directory.EnumerateFiles(securityFolder, "*.cs"))
            {
                var source = File.ReadAllText(file);

                Assert.Contains(
                    "namespace IdentityAccess.Api.Security",
                    source,
                    StringComparison.Ordinal);
            }
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
