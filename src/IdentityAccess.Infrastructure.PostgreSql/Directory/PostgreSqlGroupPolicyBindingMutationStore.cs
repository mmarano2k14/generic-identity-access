using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>
    /// Implements atomic PostgreSQL group-policy binding creation using one statement and one
    /// database snapshot.
    /// </summary>
    internal sealed class PostgreSqlGroupPolicyBindingMutationStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IGroupPolicyBindingMutationStore
    {
        /// <inheritdoc />
        public async Task<bool> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            GroupPolicyBinding binding,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(binding);
            PostgreSqlDirectoryGuard.EnsureScope(route, binding.Group.Tenant.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                WITH eligible AS
                (
                    SELECT 1
                    FROM identity_access.user_groups AS g
                    INNER JOIN identity_access.permission_policies AS p
                        ON p.identity_scope_id = g.identity_scope_id
                       AND p.tenant_id = g.tenant_id
                       AND p.application_key = g.application_key
                    WHERE g.identity_scope_id = @scope
                      AND g.tenant_id = @tenant_id
                      AND g.application_key = @application_key
                      AND g.group_id = @group_id
                      AND g.status = @active_group_status
                      AND p.policy_id = @policy_id
                      AND p.status = @active_policy_status
                      AND
                      (
                          @resource_scope_id IS NULL
                          OR EXISTS
                          (
                              SELECT 1
                              FROM identity_access.resource_scopes AS rs
                              WHERE rs.identity_scope_id = g.identity_scope_id
                                AND rs.tenant_id = g.tenant_id
                                AND rs.application_key = g.application_key
                                AND rs.resource_scope_id = @resource_scope_id
                                AND rs.status = @active_resource_scope_status
                          )
                      )
                )
                INSERT INTO identity_access.group_policy_bindings
                    (identity_scope_id, tenant_id, application_key, group_id, policy_id,
                     resource_scope_id, include_descendants)
                SELECT @scope, @tenant_id, @application_key, @group_id, @policy_id,
                       @resource_scope_id, @include_descendants
                FROM eligible
                RETURNING 1;
                """, connection);

            command.Parameters.AddWithValue("scope", binding.Group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", binding.Group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", binding.Group.Application.Value);
            command.Parameters.AddWithValue("group_id", binding.Group.GroupId);
            command.Parameters.AddWithValue("policy_id", binding.Policy.PolicyId);
            command.Parameters.AddWithValue("active_group_status", (short)GroupStatus.Active);
            command.Parameters.AddWithValue("active_policy_status", (short)PolicyStatus.Active);
            command.Parameters.AddWithValue(
                "active_resource_scope_status",
                (short)ResourceScopeStatus.Active);

            var scopeParameter = command.Parameters.Add("resource_scope_id", NpgsqlDbType.Uuid);
            scopeParameter.Value = (object?)binding.TargetScope?.ResourceScopeId ?? DBNull.Value;

            command.Parameters.AddWithValue("include_descendants", binding.IncludeDescendants);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is not null and not DBNull;
        }
    }
}
