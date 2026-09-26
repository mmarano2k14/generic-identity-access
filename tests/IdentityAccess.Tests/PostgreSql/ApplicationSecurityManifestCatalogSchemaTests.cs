namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class ApplicationSecurityManifestCatalogSchemaTests
    {
        [Fact]
        public void Manifest_catalog_migration_persists_context_fingerprint_and_mutation_audit()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0021_application_security_manifest_catalog.sql");
            var sql = File.ReadAllText(path);

            Assert.Contains("application_security_model_registrations", sql, StringComparison.Ordinal);
            Assert.Contains("application_security_namespaces", sql, StringComparison.Ordinal);
            Assert.Contains("rbac_project", sql, StringComparison.Ordinal);
            Assert.Contains("rbac_namespace", sql, StringComparison.Ordinal);
            Assert.Contains("manifest_sha256", sql, StringComparison.Ordinal);
            Assert.Contains("capture_security_mutation", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("connection_string", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln"))) return current.FullName;
                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
