namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class PostgreSqlRecoverySchemaTests
    {
        [Fact]
        public void Recovery_migration_persists_hashes_only_outside_generic_authenticator_table()
        {
            var root = FindRepositoryRoot();
            var migration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0016_recovery_provider.sql"));
            var genericMigration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0014_mfa_provider_foundation.sql"));

            Assert.Contains("identity_access.recovery_code_sets", migration, StringComparison.Ordinal);
            Assert.Contains("identity_access.recovery_codes", migration, StringComparison.Ordinal);
            Assert.Contains("code_hash bytea NOT NULL", migration, StringComparison.Ordinal);
            Assert.Contains("consumed_at timestamptz NULL", migration, StringComparison.Ordinal);
            Assert.DoesNotContain("raw_code", migration, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("recovery_codes", genericMigration, StringComparison.Ordinal);
            Assert.DoesNotContain("recovery_code", genericMigration, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "IdentityAccess.sln")))
                    return directory.FullName;
                directory = directory.Parent;
            }

            throw new InvalidOperationException("Repository root containing IdentityAccess.sln was not found.");
        }
    }
}
