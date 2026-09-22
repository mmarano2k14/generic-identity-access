using IdentityAccess.Infrastructure.PostgreSql;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlResourceScopeSchemaTests
    {
        [Fact]
        public void Resource_scope_migration_is_embedded()
        {
            var assembly = typeof(PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0008_resource_scope_hierarchy.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            Assert.Contains("application_scope_types", sql, StringComparison.Ordinal);
            Assert.Contains("resource_scopes", sql, StringComparison.Ordinal);
            Assert.Contains("include_descendants", sql, StringComparison.Ordinal);
            Assert.Contains("binding_target_key", sql, StringComparison.Ordinal);
        }
    }
}
