using System.Reflection;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlSchemaContractTests
    {
        [Fact]
        public void Initial_schema_is_embedded_and_scope_boundaries_are_part_of_relational_keys()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name => name.EndsWith("0001_identity_directory.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            Assert.Contains("PRIMARY KEY (identity_scope_id, user_id)", sql, StringComparison.Ordinal);
            Assert.Contains("PRIMARY KEY (identity_scope_id, tenant_id)", sql, StringComparison.Ordinal);
            Assert.Contains("UNIQUE (identity_scope_id, tenant_id, membership_id)", sql, StringComparison.Ordinal);
            Assert.Contains("(identity_scope_id, tenant_id, tenant_membership_id)", sql, StringComparison.Ordinal);
            Assert.Contains("row_version bigint NOT NULL DEFAULT 1", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("generic_identity_access_default", sql, StringComparison.Ordinal);
        }

        [Fact]
        public void Rbac_capability_alignment_migration_is_embedded_and_renames_persisted_segments()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0005_rbac_capability_alignment.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            Assert.Contains("capability_feature", sql, StringComparison.Ordinal);
            Assert.Contains("RENAME COLUMN capability_namespace TO capability_resource", sql, StringComparison.Ordinal);
            Assert.Contains("RENAME COLUMN capability_resource TO capability_feature", sql, StringComparison.Ordinal);
        }

        [Fact]
        public async Task User_store_rejects_record_scope_that_does_not_match_the_immutable_route()
        {
            var routeScope = Guid.NewGuid();
            var recordScope = Guid.NewGuid();
            var route = Route(routeScope);
            var subject = new SubjectReference(recordScope, Guid.NewGuid());
            var store = new PostgreSqlUserDirectoryStore(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetAsync(route, subject, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Concurrency_exception_does_not_expose_record_or_storage_identifiers()
        {
            var error = new IdentityConcurrencyException();
            Assert.Equal("The identity record was modified by another operation.", error.Message);
            Assert.DoesNotContain("database", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("user", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Versioned_record_keeps_the_version_observed_by_one_operation()
        {
            var subject = new SubjectReference(Guid.NewGuid(), Guid.NewGuid());
            var user = new User(subject, "Example User");
            var record = new VersionedRecord<User>(user, 7);

            Assert.Same(user, record.Value);
            Assert.Equal(7, record.Version);
        }

        /// <summary>
        /// Verifies that migration metadata records a checksum and that checksum drift is a
        /// dedicated storage-integrity failure.
        /// </summary>
        [Fact]
        public void Schema_migrator_tracks_migration_checksums()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "PostgreSqlSchemaMigrator.cs"));

            Assert.Contains("checksum char(64)", source, StringComparison.Ordinal);
            Assert.Contains(
                "PostgreSqlStorageFailure.MigrationIntegrityViolation",
                source,
                StringComparison.Ordinal);
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

        private static ResolvedDatabaseRoute Route(Guid scope) => new(
            new DatabaseRouteRequest(new ApplicationKey("app-a"), scope),
            "identity-default",
            new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
            1,
            1);


    }
}
