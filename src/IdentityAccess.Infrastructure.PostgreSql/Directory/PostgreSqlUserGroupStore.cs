using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL user group.</summary>
    internal sealed class PostgreSqlUserGroupStore(IIdentityDatabaseConnectionFactory connectionFactory) : IUserGroupStore
    {
        /// <summary>Gets the requested user group record from the resolved database route.</summary>
        public async Task<VersionedRecord<UserGroup>?> GetAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT display_name, status, row_version
                FROM identity_access.user_groups
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND group_id = @group_id;
                """, connection);
            AddIdentity(command, group);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            var value = new UserGroup(group, reader.GetString(0), (GroupStatus)reader.GetInt16(1));
            return new VersionedRecord<UserGroup>(value, reader.GetInt64(2));
        }

        /// <summary>Lists a bounded window of user groups in the resolved database route.</summary>
        public async Task<IReadOnlyList<VersionedRecord<UserGroup>>> ListAsync(ResolvedDatabaseRoute route,
            TenantReference tenant, ApplicationKey application, string? search, int offset, int limit,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT group_id, display_name, status, row_version
                FROM identity_access.user_groups
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND (@search_pattern IS NULL
                       OR lower(display_name) LIKE @search_pattern
                       OR group_id = @search_id)
                ORDER BY group_id
                LIMIT @limit OFFSET @offset;
                """, connection);
            command.Parameters.AddWithValue("scope", tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.TenantId);
            command.Parameters.AddWithValue("application_key", application.Value);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("limit", limit);
            command.Parameters.AddWithValue("offset", offset);
            var records = new List<VersionedRecord<UserGroup>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var reference = new GroupReference(tenant, application, reader.GetGuid(0));
                var group = new UserGroup(reference, reader.GetString(1), (GroupStatus)reader.GetInt16(2));
                records.Add(new VersionedRecord<UserGroup>(group, reader.GetInt64(3)));
            }
            return records;
        }

        /// <summary>Creates a user group record in the resolved database route.</summary>
        public async Task<VersionedRecord<UserGroup>> CreateAsync(ResolvedDatabaseRoute route, UserGroup group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.user_groups
                    (identity_scope_id, tenant_id, application_key, group_id, display_name, status)
                VALUES (@scope, @tenant_id, @application_key, @group_id, @display_name, @status)
                RETURNING row_version;
                """, connection);
            AddIdentity(command, group.Reference);
            command.Parameters.AddWithValue("display_name", group.DisplayName);
            command.Parameters.AddWithValue("status", (short)group.Status);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted group did not return a row version."));
            return new VersionedRecord<UserGroup>(group, version);
        }

        /// <summary>Updates a user group record using optimistic concurrency.</summary>
        public async Task<VersionedRecord<UserGroup>> UpdateAsync(ResolvedDatabaseRoute route, UserGroup group,
            long expectedVersion, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.user_groups
                SET display_name = @display_name,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
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
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<UserGroup>(group, (long)result);
        }

        private static void AddIdentity(NpgsqlCommand command, GroupReference group)
        {
            command.Parameters.AddWithValue("scope", group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);
        }
    }
}
