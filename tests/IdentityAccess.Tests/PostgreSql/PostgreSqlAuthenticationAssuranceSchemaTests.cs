namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>Protects the durable session/OIDC assurance columns and their initial backfill.</summary>
    public sealed class PostgreSqlAuthenticationAssuranceSchemaTests
    {
        [Fact]
        public void Session_assurance_migration_is_bounded_and_backfills_password_only_state()
        {
            var root = FindRepositoryRoot();
            var migration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0019_session_authentication_assurance.sql"));

            Assert.Contains("ALTER TABLE identity_access.user_sessions", migration, StringComparison.Ordinal);
            Assert.Contains("assurance_level smallint", migration, StringComparison.Ordinal);
            Assert.Contains("assurance_methods text[]", migration, StringComparison.Ordinal);
            Assert.Contains("assurance_verified_at timestamptz", migration, StringComparison.Ordinal);
            Assert.Contains("ARRAY['pwd']::text[]", migration, StringComparison.Ordinal);
            Assert.Contains("assurance_level IN (1, 2)", migration, StringComparison.Ordinal);
            Assert.Contains("ALTER TABLE identity_access.oidc_authorization_codes", migration, StringComparison.Ordinal);
            Assert.Contains("ALTER TABLE identity_access.oidc_refresh_tokens", migration, StringComparison.Ordinal);
            Assert.DoesNotContain("factor_secret", migration, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("session_token", migration, StringComparison.OrdinalIgnoreCase);
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
