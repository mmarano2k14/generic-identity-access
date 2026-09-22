using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Persists identity-scope administration groups in PostgreSQL.</summary>
    internal sealed class PostgreSqlIdentityScopeAdministrationGroupStore(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IIdentityScopeAdministrationGroupStore
    {
        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationGroup>?> GetAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group,
            CancellationToken cancellationToken)
        {
            Ensure(route, group);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT display_name, status, row_version
                FROM identity_access.identity_scope_administration_groups
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND group_id = @group_id;
                """, connection);

            AddIdentity(command, group);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            return new VersionedRecord<IdentityScopeAdministrationGroup>(
                new IdentityScopeAdministrationGroup(
                    group,
                    reader.GetString(0),
                    (GroupStatus)reader.GetInt16(1)),
                reader.GetInt64(2));
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationGroup>> CreateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroup group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            Ensure(route, group.Reference);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.identity_scope_administration_groups
                    (identity_scope_id, application_key, group_id, display_name, status)
                VALUES
                    (@scope, @application_key, @group_id, @display_name, @status)
                RETURNING row_version;
                """, connection);

            AddIdentity(command, group.Reference);
            command.Parameters.AddWithValue("display_name", group.DisplayName);
            command.Parameters.AddWithValue("status", (short)group.Status);

            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Inserted administration group returned no version."));

            return new VersionedRecord<IdentityScopeAdministrationGroup>(group, version);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationGroup>> UpdateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroup group,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            Ensure(route, group.Reference);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.identity_scope_administration_groups
                SET display_name = @display_name,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND group_id = @group_id
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection);

            AddIdentity(command, group.Reference);
            command.Parameters.AddWithValue("display_name", group.DisplayName);
            command.Parameters.AddWithValue("status", (short)group.Status);
            command.Parameters.AddWithValue("expected_version", expectedVersion);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull)
                throw new IdentityConcurrencyException();

            return new VersionedRecord<IdentityScopeAdministrationGroup>(group, (long)result);
        }

        private static void Ensure(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.IdentityScopeId);

            if (route.Request.Application != group.Application)
                throw new InvalidOperationException("Administration group application does not match the route.");
        }

        private static void AddIdentity(
            NpgsqlCommand command,
            IdentityScopeAdministrationGroupReference group)
        {
            command.Parameters.AddWithValue("scope", group.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);
        }
    }
}
