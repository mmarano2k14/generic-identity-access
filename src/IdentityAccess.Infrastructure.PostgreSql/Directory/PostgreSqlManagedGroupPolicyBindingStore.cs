using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>PostgreSQL persistence for tenant-scoped bindings to shared managed-policy versions.</summary>
    internal sealed class PostgreSqlManagedGroupPolicyBindingStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IManagedGroupPolicyBindingStore
    {
        /// <inheritdoc />
        public async Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            ManagedGroupPolicyBinding binding,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(binding);
            PostgreSqlDirectoryGuard.EnsureScope(route, binding.Group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.managed_group_policy_bindings
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND group_id = @group_id
                  AND policy_id = @policy_id
                  AND policy_version = @policy_version
                  AND resource_scope_id IS NOT DISTINCT FROM @resource_scope_id;
                """, connection);
            AddIdentity(command, binding);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ManagedGroupPolicyBinding>> ListAsync(
            ResolvedDatabaseRoute route,
            GroupReference group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT policy_id, policy_version, resource_scope_id, include_descendants
                FROM identity_access.managed_group_policy_bindings
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND group_id = @group_id
                ORDER BY policy_id, policy_version, resource_scope_id NULLS FIRST;
                """, connection);
            command.Parameters.AddWithValue("scope", group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);

            var result = new List<ManagedGroupPolicyBinding>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var policy = new ManagedPolicyVersionReference(
                    new ManagedPolicyReference(group.Tenant.IdentityScopeId, group.Application, reader.GetGuid(0)),
                    reader.GetInt32(1));
                var target = reader.IsDBNull(2)
                    ? null
                    : new ResourceScopeReference(group.Tenant, group.Application, reader.GetGuid(2));
                result.Add(ManagedGroupPolicyBinding.Restore(group, policy, target, reader.GetBoolean(3)));
            }

            return result.AsReadOnly();
        }

        private static void AddIdentity(NpgsqlCommand command, ManagedGroupPolicyBinding binding)
        {
            command.Parameters.AddWithValue("scope", binding.Group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", binding.Group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", binding.Group.Application.Value);
            command.Parameters.AddWithValue("group_id", binding.Group.GroupId);
            command.Parameters.AddWithValue("policy_id", binding.PolicyVersion.Policy.PolicyId);
            command.Parameters.AddWithValue("policy_version", binding.PolicyVersion.Version);
            var target = command.Parameters.Add("resource_scope_id", NpgsqlDbType.Uuid);
            target.Value = (object?)binding.TargetScope?.ResourceScopeId ?? DBNull.Value;
        }
    }
}
