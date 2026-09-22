using System.Security.Cryptography;
using System.Text;

namespace IdentityAccess.Infrastructure.PostgreSql
{
    /// <summary>
    /// Computes a platform-independent SHA-256 checksum for migration SQL.
    /// </summary>
    internal static class MigrationChecksum
    {
        /// <summary>
        /// Computes the lowercase SHA-256 checksum after normalizing line endings to LF.
        /// </summary>
        internal static string Compute(string sql)
        {
            ArgumentNullException.ThrowIfNull(sql);

            var canonical = sql
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal);

            var bytes = Encoding.UTF8.GetBytes(canonical);
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
    }
}
