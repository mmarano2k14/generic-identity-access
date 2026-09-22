using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL group policy binding.</summary>
    internal sealed class PostgreSqlGroupPolicyBindingStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IGroupPolicyBindingStore
    {
        /// <summary>Adds a group policy binding record to the resolved database route.</summary>
        public async Task AddAsync(ResolvedDatabaseRoute route, GroupPolicyBinding binding,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(binding);
            PostgreSqlDirectoryGuard.EnsureScope(route, binding.Group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.group_policy_bindings
                    (identity_scope_id, tenant_id, application_key, group_id, policy_id,
                     resource_scope_id, include_descendants)
                VALUES (@scope, @tenant_id, @application_key, @group_id, @policy_id,
                        @resource_scope_id, @include_descendants);
                """, connection);
            AddIdentity(command, binding);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Removes a group policy binding record from the resolved database route.</summary>
        public async Task<bool> RemoveAsync(ResolvedDatabaseRoute route, GroupPolicyBinding binding,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(binding);
            PostgreSqlDirectoryGuard.EnsureScope(route, binding.Group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.group_policy_bindings
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND group_id = @group_id
                  AND policy_id = @policy_id
                  AND resource_scope_id IS NOT DISTINCT FROM @resource_scope_id;
                """, connection);
            AddIdentity(command, binding);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }

        /// <summary>Lists group policy binding records for the supplied scope.</summary>
        public async Task<IReadOnlyList<GroupPolicyBinding>> ListAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT policy_id, resource_scope_id, include_descendants
                FROM identity_access.group_policy_bindings
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND group_id = @group_id
                ORDER BY policy_id, resource_scope_id NULLS FIRST;
                """, connection);
            command.Parameters.AddWithValue("scope", group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);
            var result = new List<GroupPolicyBinding>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var policy = new PermissionPolicyReference(group.Tenant, group.Application, reader.GetGuid(0));
                var target = reader.IsDBNull(1) ? null : new ResourceScopeReference(group.Tenant, group.Application, reader.GetGuid(1));
                result.Add(GroupPolicyBinding.Restore(group, policy, target, reader.GetBoolean(2)));
            }
            return result.AsReadOnly();
        }

        private static void AddIdentity(NpgsqlCommand command, GroupPolicyBinding binding)
        {
            command.Parameters.AddWithValue("scope", binding.Group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", binding.Group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", binding.Group.Application.Value);
            command.Parameters.AddWithValue("group_id", binding.Group.GroupId);
            command.Parameters.AddWithValue("policy_id", binding.Policy.PolicyId);
            var target = command.Parameters.Add("resource_scope_id", NpgsqlDbType.Uuid);
            target.Value = (object?)binding.TargetScope?.ResourceScopeId ?? DBNull.Value;
            command.Parameters.AddWithValue("include_descendants", binding.IncludeDescendants);
        }
    }
}
