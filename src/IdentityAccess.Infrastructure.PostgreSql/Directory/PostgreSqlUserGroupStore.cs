using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Provides persistence operations for PostgreSQL user groups.</summary>
    internal sealed class PostgreSqlUserGroupStore(IIdentityDatabaseConnectionFactory connectionFactory) : IUserGroupStore
    {
        public async Task<VersionedRecord<UserGroup>?> GetAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT display_name, status, is_template, row_version
                FROM identity_access.user_groups
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND group_id = @group_id;
                """, connection);
            AddIdentity(command, group);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            return Read(group, reader);
        }

        public async Task<IReadOnlyList<VersionedRecord<UserGroup>>> ListAsync(ResolvedDatabaseRoute route,
            TenantReference tenant, ApplicationKey application, string? search, int offset, int limit,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT group_id, display_name, status, is_template, row_version
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
                var group = new UserGroup(reference, reader.GetString(1), (GroupStatus)reader.GetInt16(2), reader.GetBoolean(3));
                records.Add(new VersionedRecord<UserGroup>(group, reader.GetInt64(4)));
            }
            return records;
        }

        public async Task<IReadOnlyList<VersionedRecord<UserGroup>>> ListTemplatesAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            bool activeOnly,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT tenant_id, group_id, display_name, status, is_template, row_version
                FROM identity_access.user_groups
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND is_template = TRUE
                  AND (@active_only = FALSE OR status = @active_status)
                  AND (@search_pattern IS NULL
                       OR lower(display_name) LIKE @search_pattern
                       OR group_id = @search_id)
                ORDER BY lower(display_name), tenant_id, group_id
                LIMIT @limit OFFSET @offset;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            command.Parameters.AddWithValue("application_key", application.Value);
            command.Parameters.AddWithValue("active_only", activeOnly);
            command.Parameters.AddWithValue("active_status", (short)GroupStatus.Active);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("limit", limit);
            command.Parameters.AddWithValue("offset", offset);
            var records = new List<VersionedRecord<UserGroup>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var tenant = new TenantReference(identityScopeId, reader.GetGuid(0));
                var reference = new GroupReference(tenant, application, reader.GetGuid(1));
                var group = new UserGroup(reference, reader.GetString(2), (GroupStatus)reader.GetInt16(3), reader.GetBoolean(4));
                records.Add(new VersionedRecord<UserGroup>(group, reader.GetInt64(5)));
            }
            return records;
        }

        public async Task<VersionedRecord<UserGroup>> CreateAsync(ResolvedDatabaseRoute route, UserGroup group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.user_groups
                    (identity_scope_id, tenant_id, application_key, group_id, display_name, status, is_template)
                VALUES (@scope, @tenant_id, @application_key, @group_id, @display_name, @status, @is_template)
                RETURNING row_version;
                """, connection);
            AddIdentity(command, group.Reference);
            command.Parameters.AddWithValue("display_name", group.DisplayName);
            command.Parameters.AddWithValue("status", (short)group.Status);
            command.Parameters.AddWithValue("is_template", group.IsTemplate);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted group did not return a row version."));
            return new VersionedRecord<UserGroup>(group, version);
        }

        public async Task<VersionedRecord<UserGroup>?> CreateFromTemplateAsync(
            ResolvedDatabaseRoute route,
            GroupReference sourceGroup,
            GroupReference targetGroup,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(sourceGroup);
            ArgumentNullException.ThrowIfNull(targetGroup);
            PostgreSqlDirectoryGuard.EnsureScope(route, sourceGroup.Tenant.IdentityScopeId);
            PostgreSqlDirectoryGuard.EnsureScope(route, targetGroup.Tenant.IdentityScopeId);
            if (sourceGroup.Tenant.IdentityScopeId != targetGroup.Tenant.IdentityScopeId ||
                sourceGroup.Application != targetGroup.Application)
                throw new ArgumentException("Template source and target must share identity scope and application.");

            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            string displayName;
            await using (var source = new NpgsqlCommand("""
                SELECT display_name
                FROM identity_access.user_groups
                WHERE identity_scope_id = @scope
                  AND tenant_id = @source_tenant_id
                  AND application_key = @application_key
                  AND group_id = @source_group_id
                  AND is_template = TRUE
                  AND status = @active_status
                FOR SHARE;
                """, connection, transaction))
            {
                source.Parameters.AddWithValue("scope", sourceGroup.Tenant.IdentityScopeId);
                source.Parameters.AddWithValue("source_tenant_id", sourceGroup.Tenant.TenantId);
                source.Parameters.AddWithValue("application_key", sourceGroup.Application.Value);
                source.Parameters.AddWithValue("source_group_id", sourceGroup.GroupId);
                source.Parameters.AddWithValue("active_status", (short)GroupStatus.Active);
                var value = await source.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (value is null or DBNull)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return null;
                }
                displayName = (string)value;
            }

            await using (var portability = new NpgsqlCommand("""
                SELECT COUNT(*)
                FROM identity_access.managed_group_policy_bindings AS b
                WHERE b.identity_scope_id = @scope
                  AND b.tenant_id = @source_tenant_id
                  AND b.application_key = @application_key
                  AND b.group_id = @source_group_id
                  AND b.resource_scope_id IS NOT NULL
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM identity_access.resource_scopes AS rs
                      WHERE rs.identity_scope_id = @scope
                        AND rs.tenant_id = @target_tenant_id
                        AND rs.application_key = @application_key
                        AND rs.resource_scope_id = b.resource_scope_id
                        AND rs.status = @active_resource_status
                  );
                """, connection, transaction))
            {
                portability.Parameters.AddWithValue("scope", sourceGroup.Tenant.IdentityScopeId);
                portability.Parameters.AddWithValue("source_tenant_id", sourceGroup.Tenant.TenantId);
                portability.Parameters.AddWithValue("target_tenant_id", targetGroup.Tenant.TenantId);
                portability.Parameters.AddWithValue("application_key", sourceGroup.Application.Value);
                portability.Parameters.AddWithValue("source_group_id", sourceGroup.GroupId);
                portability.Parameters.AddWithValue("active_resource_status", (short)ResourceScopeStatus.Active);
                var incompatible = (long)(await portability.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L);
                if (incompatible != 0)
                    throw new InvalidOperationException("Template policy bindings reference resource scopes that do not exist in the target tenant.");
            }

            long version;
            await using (var insert = new NpgsqlCommand("""
                INSERT INTO identity_access.user_groups
                    (identity_scope_id, tenant_id, application_key, group_id, display_name, status, is_template)
                VALUES (@scope, @target_tenant_id, @application_key, @target_group_id, @display_name, @active_status, FALSE)
                RETURNING row_version;
                """, connection, transaction))
            {
                insert.Parameters.AddWithValue("scope", targetGroup.Tenant.IdentityScopeId);
                insert.Parameters.AddWithValue("target_tenant_id", targetGroup.Tenant.TenantId);
                insert.Parameters.AddWithValue("application_key", targetGroup.Application.Value);
                insert.Parameters.AddWithValue("target_group_id", targetGroup.GroupId);
                insert.Parameters.AddWithValue("display_name", displayName);
                insert.Parameters.AddWithValue("active_status", (short)GroupStatus.Active);
                version = (long)(await insert.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The cloned group did not return a row version."));
            }

            await using (var bindings = new NpgsqlCommand("""
                INSERT INTO identity_access.managed_group_policy_bindings
                    (identity_scope_id, tenant_id, application_key, group_id, policy_id, policy_version,
                     resource_scope_id, include_descendants)
                SELECT b.identity_scope_id, @target_tenant_id, b.application_key, @target_group_id,
                       b.policy_id, b.policy_version, b.resource_scope_id, b.include_descendants
                FROM identity_access.managed_group_policy_bindings AS b
                WHERE b.identity_scope_id = @scope
                  AND b.tenant_id = @source_tenant_id
                  AND b.application_key = @application_key
                  AND b.group_id = @source_group_id;
                """, connection, transaction))
            {
                bindings.Parameters.AddWithValue("scope", sourceGroup.Tenant.IdentityScopeId);
                bindings.Parameters.AddWithValue("source_tenant_id", sourceGroup.Tenant.TenantId);
                bindings.Parameters.AddWithValue("target_tenant_id", targetGroup.Tenant.TenantId);
                bindings.Parameters.AddWithValue("application_key", sourceGroup.Application.Value);
                bindings.Parameters.AddWithValue("source_group_id", sourceGroup.GroupId);
                bindings.Parameters.AddWithValue("target_group_id", targetGroup.GroupId);
                await bindings.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new VersionedRecord<UserGroup>(new UserGroup(targetGroup, displayName, GroupStatus.Active, false), version);
        }

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
                    is_template = @is_template,
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
            command.Parameters.AddWithValue("is_template", group.IsTemplate);
            command.Parameters.AddWithValue("expected_version", expectedVersion);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<UserGroup>(group, (long)result);
        }

        private static VersionedRecord<UserGroup> Read(GroupReference reference, NpgsqlDataReader reader)
        {
            var group = new UserGroup(reference, reader.GetString(0), (GroupStatus)reader.GetInt16(1), reader.GetBoolean(2));
            return new VersionedRecord<UserGroup>(group, reader.GetInt64(3));
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
