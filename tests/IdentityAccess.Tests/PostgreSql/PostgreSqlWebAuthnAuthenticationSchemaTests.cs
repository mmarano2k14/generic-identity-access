namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class PostgreSqlWebAuthnAuthenticationSchemaTests
    {
        [Fact]
        public void WebAuthn_authentication_migration_persists_hash_only_challenges()
        {
            var root = FindRepositoryRoot();
            var migration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0018_webauthn_authentication.sql"));

            Assert.Contains("identity_access.webauthn_authentication_challenges", migration, StringComparison.Ordinal);
            Assert.Contains("challenge_hash bytea NOT NULL", migration, StringComparison.Ordinal);
            Assert.Contains("consumed_at timestamptz NULL", migration, StringComparison.Ordinal);
            Assert.Contains("fk_webauthn_authentication_challenges_user", migration, StringComparison.Ordinal);
            Assert.DoesNotContain("raw_challenge", migration, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("private_key", migration, StringComparison.OrdinalIgnoreCase);
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
