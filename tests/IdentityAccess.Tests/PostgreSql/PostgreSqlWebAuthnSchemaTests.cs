namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class PostgreSqlWebAuthnSchemaTests
    {
        [Fact]
        public void WebAuthn_migration_persists_only_public_credential_material_outside_generic_table()
        {
            var root = FindRepositoryRoot();
            var migration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0017_webauthn_registration.sql"));
            var genericMigration = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0014_mfa_provider_foundation.sql"));

            Assert.Contains("identity_access.webauthn_registration_challenges", migration, StringComparison.Ordinal);
            Assert.Contains("challenge_hash bytea NOT NULL", migration, StringComparison.Ordinal);
            Assert.Contains("cose_public_key bytea NOT NULL", migration, StringComparison.Ordinal);
            Assert.Contains("credential_id bytea NOT NULL", migration, StringComparison.Ordinal);
            Assert.DoesNotContain("private_key", migration, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("webauthn_credentials", genericMigration, StringComparison.Ordinal);
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
