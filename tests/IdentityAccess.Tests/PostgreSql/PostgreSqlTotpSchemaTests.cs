namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class PostgreSqlTotpSchemaTests
    {
        [Fact]
        public void Totp_migration_keeps_provider_secret_out_of_generic_authenticator_table()
        {
            var root = FindRepositoryRoot();
            var migration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0015_totp_provider.sql"));
            var genericMigration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0014_mfa_provider_foundation.sql"));

            Assert.Contains("identity_access.totp_authenticators", migration, StringComparison.Ordinal);
            Assert.Contains("protected_secret bytea NOT NULL", migration, StringComparison.Ordinal);
            Assert.Contains("last_accepted_time_step bigint NULL", migration, StringComparison.Ordinal);
            Assert.DoesNotContain("totp_authenticators", genericMigration, StringComparison.Ordinal);
            Assert.DoesNotContain("totp_secret", genericMigration, StringComparison.OrdinalIgnoreCase);
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
