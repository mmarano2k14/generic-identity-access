namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>Verifies the secret-safe PostgreSQL security audit schema.</summary>
    public sealed class PostgreSqlSecurityAuditContractTests
    {
        /// <summary>Verifies audit columns include correlation and exclude credential/token data.</summary>
        [Fact]
        public void Security_audit_migration_contains_only_safe_columns()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0009_security_audit.sql"));

            Assert.Contains("correlation_id", source, StringComparison.Ordinal);
            Assert.Contains("event_type", source, StringComparison.Ordinal);
            Assert.DoesNotContain("password_hash", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("session_token", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("connection_string", source, StringComparison.OrdinalIgnoreCase);
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
