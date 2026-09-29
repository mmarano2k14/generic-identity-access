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

        public async Task<IReadOnlyList<GroupTemplateResourceScopeRequirement>> ListTemplateScopeRequirementsAsync(
            ResolvedDatabaseRoute route,
            GroupReference sourceGroup,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(sourceGroup);
            PostgreSqlDirectoryGuard.EnsureScope(route, sourceGroup.Tenant.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT DISTINCT
                       rs.resource_scope_id,
                       rs.scope_model_version,
                       rs.scope_type_key,
                       rs.display_name
                FROM identity_access.user_groups AS g
                INNER JOIN identity_access.managed_group_policy_bindings AS b
                    ON b.identity_scope_id = g.identity_scope_id
                   AND b.tenant_id = g.tenant_id
                   AND b.application_key = g.application_key
                   AND b.group_id = g.group_id
                INNER JOIN identity_access.resource_scopes AS rs
                    ON rs.identity_scope_id = b.identity_scope_id
                   AND rs.tenant_id = b.tenant_id
                   AND rs.application_key = b.application_key
                   AND rs.resource_scope_id = b.resource_scope_id
                WHERE g.identity_scope_id = @scope
                  AND g.tenant_id = @source_tenant_id
                  AND g.application_key = @application_key
                  AND g.group_id = @source_group_id
                  AND g.is_template = TRUE
                  AND g.status = @active_group_status
                  AND b.resource_scope_id IS NOT NULL
                ORDER BY rs.scope_type_key, rs.display_name, rs.resource_scope_id;
                """, connection);
            command.Parameters.AddWithValue("scope", sourceGroup.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("source_tenant_id", sourceGroup.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", sourceGroup.Application.Value);
            command.Parameters.AddWithValue("source_group_id", sourceGroup.GroupId);
            command.Parameters.AddWithValue("active_group_status", (short)GroupStatus.Active);

            var result = new List<GroupTemplateResourceScopeRequirement>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                result.Add(new GroupTemplateResourceScopeRequirement(
                    reader.GetGuid(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetString(3)));
            }

            return result.AsReadOnly();
        }

        public async Task<VersionedRecord<UserGroup>?> CreateFromTemplateAsync(
            ResolvedDatabaseRoute route,
            GroupReference sourceGroup,
            GroupReference targetGroup,
            IReadOnlyDictionary<Guid, Guid> resourceScopeMappings,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(sourceGroup);
            ArgumentNullException.ThrowIfNull(targetGroup);
            ArgumentNullException.ThrowIfNull(resourceScopeMappings);
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

            var sourceBindings = new List<(Guid PolicyId, int PolicyVersion, Guid? ResourceScopeId, bool IncludeDescendants)>();
            await using (var bindingQuery = new NpgsqlCommand("""
                SELECT policy_id, policy_version, resource_scope_id, include_descendants
                FROM identity_access.managed_group_policy_bindings
                WHERE identity_scope_id = @scope
                  AND tenant_id = @source_tenant_id
                  AND application_key = @application_key
                  AND group_id = @source_group_id
                ORDER BY policy_id, policy_version, resource_scope_id NULLS FIRST;
                """, connection, transaction))
            {
                bindingQuery.Parameters.AddWithValue("scope", sourceGroup.Tenant.IdentityScopeId);
                bindingQuery.Parameters.AddWithValue("source_tenant_id", sourceGroup.Tenant.TenantId);
                bindingQuery.Parameters.AddWithValue("application_key", sourceGroup.Application.Value);
                bindingQuery.Parameters.AddWithValue("source_group_id", sourceGroup.GroupId);
                await using var reader = await bindingQuery.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    sourceBindings.Add((
                        reader.GetGuid(0),
                        reader.GetInt32(1),
                        reader.IsDBNull(2) ? null : reader.GetGuid(2),
                        reader.GetBoolean(3)));
                }
            }

            var requiredSourceScopes = sourceBindings
                .Where(binding => binding.ResourceScopeId.HasValue)
                .Select(binding => binding.ResourceScopeId!.Value)
                .Distinct()
                .ToArray();

            foreach (var sourceScopeId in requiredSourceScopes)
            {
                var targetScopeId = ResolveTargetScopeId(sourceGroup, targetGroup, resourceScopeMappings, sourceScopeId);
                await ValidateScopeMappingAsync(
                    connection,
                    transaction,
                    sourceGroup,
                    targetGroup,
                    sourceScopeId,
                    targetScopeId,
                    cancellationToken).ConfigureAwait(false);
            }

            var extraMapping = resourceScopeMappings.Keys.FirstOrDefault(
                sourceScopeId => !requiredSourceScopes.Contains(sourceScopeId));
            if (extraMapping != Guid.Empty)
            {
                throw new GroupTemplateScopeMappingException(
                    $"Resource-scope mapping '{extraMapping:D}' is not referenced by the reusable group's managed-policy bindings.");
            }

            var mappedBindingKeys = new HashSet<(Guid PolicyId, int PolicyVersion, Guid? ResourceScopeId)>();
            foreach (var binding in sourceBindings)
            {
                var targetScopeId = binding.ResourceScopeId is null
                    ? (Guid?)null
                    : ResolveTargetScopeId(
                        sourceGroup,
                        targetGroup,
                        resourceScopeMappings,
                        binding.ResourceScopeId.Value);
                if (!mappedBindingKeys.Add((binding.PolicyId, binding.PolicyVersion, targetScopeId)))
                {
                    throw new GroupTemplateScopeMappingException(
                        "Two source bindings would collapse onto the same policy/version/resource-scope binding in the target tenant.");
                }
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

            await using (var insertBinding = new NpgsqlCommand("""
                INSERT INTO identity_access.managed_group_policy_bindings
                    (identity_scope_id, tenant_id, application_key, group_id, policy_id, policy_version,
                     resource_scope_id, include_descendants)
                VALUES
                    (@scope, @target_tenant_id, @application_key, @target_group_id, @policy_id, @policy_version,
                     @resource_scope_id, @include_descendants);
                """, connection, transaction))
            {
                insertBinding.Parameters.AddWithValue("scope", targetGroup.Tenant.IdentityScopeId);
                insertBinding.Parameters.AddWithValue("target_tenant_id", targetGroup.Tenant.TenantId);
                insertBinding.Parameters.AddWithValue("application_key", targetGroup.Application.Value);
                insertBinding.Parameters.AddWithValue("target_group_id", targetGroup.GroupId);

                var policyId = insertBinding.Parameters.Add("policy_id", NpgsqlTypes.NpgsqlDbType.Uuid);
                var policyVersion = insertBinding.Parameters.Add("policy_version", NpgsqlTypes.NpgsqlDbType.Integer);
                var resourceScopeId = insertBinding.Parameters.Add("resource_scope_id", NpgsqlTypes.NpgsqlDbType.Uuid);
                var includeDescendants = insertBinding.Parameters.Add("include_descendants", NpgsqlTypes.NpgsqlDbType.Boolean);

                foreach (var binding in sourceBindings)
                {
                    policyId.Value = binding.PolicyId;
                    policyVersion.Value = binding.PolicyVersion;
                    resourceScopeId.Value = binding.ResourceScopeId is null
                        ? DBNull.Value
                        : ResolveTargetScopeId(
                            sourceGroup,
                            targetGroup,
                            resourceScopeMappings,
                            binding.ResourceScopeId.Value);
                    includeDescendants.Value = binding.IncludeDescendants;
                    await insertBinding.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new VersionedRecord<UserGroup>(new UserGroup(targetGroup, displayName, GroupStatus.Active, false), version);
        }

        private static Guid ResolveTargetScopeId(
            GroupReference sourceGroup,
            GroupReference targetGroup,
            IReadOnlyDictionary<Guid, Guid> resourceScopeMappings,
            Guid sourceScopeId)
        {
            if (resourceScopeMappings.TryGetValue(sourceScopeId, out var mappedScopeId) && mappedScopeId != Guid.Empty)
                return mappedScopeId;

            if (sourceGroup.Tenant.TenantId == targetGroup.Tenant.TenantId)
                return sourceScopeId;

            throw new GroupTemplateScopeMappingException(
                $"Reusable group resource scope '{sourceScopeId:D}' requires an explicit target-tenant scope mapping.");
        }

        private static async Task ValidateScopeMappingAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            GroupReference sourceGroup,
            GroupReference targetGroup,
            Guid sourceScopeId,
            Guid targetScopeId,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT
                    source.scope_model_version,
                    source.scope_type_key,
                    target.scope_model_version,
                    target.scope_type_key,
                    target.status
                FROM identity_access.resource_scopes AS source
                LEFT JOIN identity_access.resource_scopes AS target
                  ON target.identity_scope_id = source.identity_scope_id
                 AND target.tenant_id = @target_tenant_id
                 AND target.application_key = source.application_key
                 AND target.resource_scope_id = @target_resource_scope_id
                WHERE source.identity_scope_id = @scope
                  AND source.tenant_id = @source_tenant_id
                  AND source.application_key = @application_key
                  AND source.resource_scope_id = @source_resource_scope_id;
                """, connection, transaction);
            command.Parameters.AddWithValue("scope", sourceGroup.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("source_tenant_id", sourceGroup.Tenant.TenantId);
            command.Parameters.AddWithValue("target_tenant_id", targetGroup.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", sourceGroup.Application.Value);
            command.Parameters.AddWithValue("source_resource_scope_id", sourceScopeId);
            command.Parameters.AddWithValue("target_resource_scope_id", targetScopeId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) || reader.IsDBNull(2))
            {
                throw new GroupTemplateScopeMappingException(
                    $"Target resource scope '{targetScopeId:D}' does not exist in the target tenant.");
            }

            var sourceModelVersion = reader.GetInt32(0);
            var sourceType = reader.GetString(1);
            var targetModelVersion = reader.GetInt32(2);
            var targetType = reader.GetString(3);
            var targetStatus = (ResourceScopeStatus)reader.GetInt16(4);

            if (targetStatus != ResourceScopeStatus.Active)
            {
                throw new GroupTemplateScopeMappingException(
                    $"Target resource scope '{targetScopeId:D}' is not active.");
            }

            if (sourceModelVersion != targetModelVersion ||
                !StringComparer.Ordinal.Equals(sourceType, targetType))
            {
                throw new GroupTemplateScopeMappingException(
                    $"Target resource scope '{targetScopeId:D}' is incompatible with source scope '{sourceScopeId:D}'. " +
                    "Scope type and model version must match.");
            }
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
