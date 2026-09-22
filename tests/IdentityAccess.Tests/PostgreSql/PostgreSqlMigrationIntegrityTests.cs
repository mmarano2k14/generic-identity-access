using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using IdentityAccess.Infrastructure.PostgreSql;

namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>
    /// Verifies deterministic migration checksums and embedded migration metadata.
    /// </summary>
    public sealed class PostgreSqlMigrationIntegrityTests
    {
        /// <summary>
        /// Verifies that canonical migration hashing is independent of CRLF versus LF line endings.
        /// </summary>
        [Fact]
        public void Migration_checksum_normalizes_line_endings()
        {
            var type = typeof(PostgreSqlSchemaMigrator).Assembly.GetType(
                "IdentityAccess.Infrastructure.PostgreSql.MigrationChecksum",
                throwOnError: true)!;

            var compute = type.GetMethod(
                "Compute",
                BindingFlags.Static | BindingFlags.NonPublic)!;

            var crlfSql = "SELECT 1;\r\nSELECT 2;\r\n";
            var lfSql = "SELECT 1;\nSELECT 2;\n";

            var crlf = Assert.IsType<string>(compute.Invoke(null, [crlfSql]));
            var lf = Assert.IsType<string>(compute.Invoke(null, [lfSql]));

            Assert.Equal(lf, crlf);
            Assert.Matches("^[0-9a-f]{64}$", lf);
        }

        /// <summary>
        /// Verifies that every embedded migration exposes a deterministic 64-character checksum.
        /// </summary>
        [Fact]
        public void Embedded_migrations_include_deterministic_checksums()
        {
            var method = typeof(PostgreSqlSchemaMigrator).GetMethod(
                "LoadMigrations",
                BindingFlags.Static | BindingFlags.NonPublic)!;

            var migrations = Assert.IsAssignableFrom<System.Collections.IEnumerable>(
                method.Invoke(null, null));

            var count = 0;
            foreach (var migration in migrations)
            {
                count++;
                var type = migration!.GetType();
                var checksum = Assert.IsType<string>(
                    type.GetProperty("Checksum")!.GetValue(migration));
                var sql = Assert.IsType<string>(
                    type.GetProperty("Sql")!.GetValue(migration));

                Assert.Matches("^[0-9a-f]{64}$", checksum);
                Assert.Equal(ReferenceChecksum(sql), checksum);
            }

            Assert.True(count >= 8);
        }

        private static string ReferenceChecksum(string sql)
        {
            var canonical = sql
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal);

            return Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
                .ToLowerInvariant();
        }
    }
}
