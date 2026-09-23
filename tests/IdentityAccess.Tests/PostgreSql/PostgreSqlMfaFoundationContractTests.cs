namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class PostgreSqlMfaFoundationContractTests
    {
        [Fact]
        public void Mfa_foundation_schema_is_provider_neutral_and_transactionally_audited()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0014_mfa_provider_foundation.sql");
            var sql = File.ReadAllText(path);

            Assert.Contains("identity_access.mfa_policies", sql, StringComparison.Ordinal);
            Assert.Contains("identity_access.mfa_policy_providers", sql, StringComparison.Ordinal);
            Assert.Contains("identity_access.user_authenticators", sql, StringComparison.Ordinal);
            Assert.Contains("trg_security_mutation_mfa_policies", sql, StringComparison.Ordinal);
            Assert.Contains("trg_security_mutation_user_authenticators", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("provider_payload", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("totp_secret", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("private_key", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("recovery_code", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                    return current.FullName;
                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
