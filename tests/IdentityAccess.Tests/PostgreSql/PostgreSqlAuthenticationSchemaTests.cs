using System.Reflection;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlAuthenticationSchemaTests
    {
        [Fact]
        public void Authentication_migration_persists_hashes_and_sessions_without_raw_session_tokens()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0007_local_authentication_foundation.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            Assert.Contains("identity_access.password_credentials", sql, StringComparison.Ordinal);
            Assert.Contains("password_hash text NOT NULL", sql, StringComparison.Ordinal);
            Assert.Contains("normalized_login_identifier", sql, StringComparison.Ordinal);
            Assert.Contains("identity_access.user_sessions", sql, StringComparison.Ordinal);
            Assert.Contains("token_hash bytea NOT NULL", sql, StringComparison.Ordinal);
            Assert.Contains("octet_length(token_hash) = 32", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("session_token", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("plaintext", sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
