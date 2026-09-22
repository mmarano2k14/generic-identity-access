using System.Reflection;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql
{
    /// <summary>
    /// Applies embedded append-only schema migrations and verifies that already-applied migration
    /// names and checksums still match the embedded migration set.
    /// </summary>
    internal sealed class PostgreSqlSchemaMigrator(
        IIdentityDatabaseConnectionFactory connectionFactory) : IIdentitySchemaMigrator
    {
        private const long MigrationLockKey = 0x4944414343455353;

        /// <summary>
        /// Applies pending embedded migrations and fails when historical migration metadata has
        /// drifted from the embedded migration set.
        /// </summary>
        public async Task MigrateAsync(
            ResolvedDatabaseRoute route,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            cancellationToken.ThrowIfCancellationRequested();

            var migrations = LoadMigrations();

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await connection
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            await ExecuteAsync(
                connection,
                transaction,
                $"SELECT pg_advisory_xact_lock({MigrationLockKey});",
                cancellationToken).ConfigureAwait(false);

            await ExecuteAsync(
                connection,
                transaction,
                """
                CREATE SCHEMA IF NOT EXISTS identity_access;
                CREATE TABLE IF NOT EXISTS identity_access.schema_migrations
                (
                    version integer PRIMARY KEY,
                    name varchar(200) NOT NULL,
                    checksum char(64) NULL,
                    applied_at timestamptz NOT NULL DEFAULT transaction_timestamp()
                );
                ALTER TABLE identity_access.schema_migrations
                    ADD COLUMN IF NOT EXISTS checksum char(64) NULL;
                """,
                cancellationToken).ConfigureAwait(false);

            var applied = await LoadAppliedAsync(
                connection,
                transaction,
                cancellationToken).ConfigureAwait(false);

            var embeddedVersions = migrations
                .Select(migration => migration.Version)
                .ToHashSet();

            if (applied.Keys.Any(version => !embeddedVersions.Contains(version)))
            {
                throw IntegrityFailure();
            }

            foreach (var migration in migrations)
            {
                if (applied.TryGetValue(migration.Version, out var existing))
                {
                    if (!string.Equals(existing.Name, migration.Name, StringComparison.Ordinal))
                    {
                        throw IntegrityFailure();
                    }

                    if (string.IsNullOrWhiteSpace(existing.Checksum))
                    {
                        await RecordChecksumAsync(
                            connection,
                            transaction,
                            migration,
                            cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (!string.Equals(
                        existing.Checksum,
                        migration.Checksum,
                        StringComparison.Ordinal))
                    {
                        throw IntegrityFailure();
                    }

                    continue;
                }

                await ExecuteAsync(
                    connection,
                    transaction,
                    migration.Sql,
                    cancellationToken).ConfigureAwait(false);

                await using var record = new NpgsqlCommand(
                    """
                    INSERT INTO identity_access.schema_migrations(version, name, checksum)
                    VALUES (@version, @name, @checksum);
                    """,
                    connection,
                    transaction);
                record.Parameters.AddWithValue("version", migration.Version);
                record.Parameters.AddWithValue("name", migration.Name);
                record.Parameters.AddWithValue("checksum", migration.Checksum);
                await record.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await ExecuteAsync(
                connection,
                transaction,
                """
                ALTER TABLE identity_access.schema_migrations
                    ALTER COLUMN checksum SET NOT NULL;
                """,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        internal static IReadOnlyList<EmbeddedMigration> LoadMigrations()
        {
            const string marker = ".Migrations.";
            var assembly = typeof(PostgreSqlSchemaMigrator).Assembly;

            return assembly.GetManifestResourceNames()
                .Where(name =>
                    name.Contains(marker, StringComparison.Ordinal) &&
                    name.EndsWith(".sql", StringComparison.Ordinal))
                .Select(name =>
                {
                    var file = name[
                        (name.LastIndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
                    var underscore = file.IndexOf('_');

                    if (underscore != 4 ||
                        !int.TryParse(file.AsSpan(0, 4), out var version))
                    {
                        throw new InvalidOperationException(
                            "Embedded PostgreSQL migrations must use NNNN_name.sql.");
                    }

                    using var stream = assembly.GetManifestResourceStream(name)
                        ?? throw new InvalidOperationException(
                            "Embedded PostgreSQL migration cannot be opened.");
                    using var reader = new StreamReader(stream);
                    var sql = reader.ReadToEnd();

                    return new EmbeddedMigration(
                        version,
                        file,
                        sql,
                        MigrationChecksum.Compute(sql));
                })
                .OrderBy(migration => migration.Version)
                .ToArray();
        }

        private static async Task<Dictionary<int, AppliedMigration>> LoadAppliedAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(
                """
                SELECT version, name, checksum
                FROM identity_access.schema_migrations
                ORDER BY version;
                """,
                connection,
                transaction);

            var result = new Dictionary<int, AppliedMigration>();
            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var migration = new AppliedMigration(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2).Trim());

                result.Add(migration.Version, migration);
            }

            return result;
        }

        private static async Task RecordChecksumAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            EmbeddedMigration migration,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(
                """
                UPDATE identity_access.schema_migrations
                SET checksum = @checksum
                WHERE version = @version
                  AND name = @name
                  AND checksum IS NULL;
                """,
                connection,
                transaction);
            command.Parameters.AddWithValue("checksum", migration.Checksum);
            command.Parameters.AddWithValue("version", migration.Version);
            command.Parameters.AddWithValue("name", migration.Name);

            if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw IntegrityFailure();
            }
        }

        private static async Task ExecuteAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            string sql,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static PostgreSqlStorageException IntegrityFailure() =>
            new(PostgreSqlStorageFailure.MigrationIntegrityViolation);
    }
}
