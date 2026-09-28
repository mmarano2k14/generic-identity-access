using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Reads published managed-policy capabilities for one active tenant group.</summary>
    internal sealed class PostgreSqlGroupCapabilityGrantReader(
        IIdentityDatabaseConnectionFactory connectionFactory) : IGroupCapabilityGrantReader
    {
        public async Task<IReadOnlyList<GroupCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            GroupReference group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                SELECT
                    mps.capability_resource,
                    mps.capability_feature,
                    mps.capability_action,
                    mgb.resource_scope_id,
                    mgb.include_descendants
                FROM identity_access.user_groups ug
                JOIN identity_access.managed_group_policy_bindings mgb
                  ON mgb.identity_scope_id = ug.identity_scope_id
                 AND mgb.tenant_id = ug.tenant_id
                 AND mgb.application_key = ug.application_key
                 AND mgb.group_id = ug.group_id
                JOIN identity_access.managed_policies mp
                  ON mp.identity_scope_id = mgb.identity_scope_id
                 AND mp.application_key = mgb.application_key
                 AND mp.policy_id = mgb.policy_id
                JOIN identity_access.managed_policy_versions mpv
                  ON mpv.identity_scope_id = mgb.identity_scope_id
                 AND mpv.application_key = mgb.application_key
                 AND mpv.policy_id = mgb.policy_id
                 AND mpv.policy_version = mgb.policy_version
                JOIN identity_access.managed_policy_statements mps
                  ON mps.identity_scope_id = mpv.identity_scope_id
                 AND mps.application_key = mpv.application_key
                 AND mps.policy_id = mpv.policy_id
                 AND mps.policy_version = mpv.policy_version
                 AND mps.model_version = mpv.model_version
                WHERE ug.identity_scope_id = @scope
                  AND ug.tenant_id = @tenant_id
                  AND ug.application_key = @application_key
                  AND ug.group_id = @group_id
                  AND ug.status = 1
                  AND mp.status = 1
                  AND mpv.published_at IS NOT NULL
                ORDER BY mp.policy_id, mgb.policy_version, mps.statement_id,
                         mgb.resource_scope_id NULLS FIRST;
                """, connection);

            command.Parameters.AddWithValue("scope", group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);

            var grants = new List<GroupCapabilityGrant>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var pattern = new CapabilityPattern(reader.GetString(0), reader.GetString(1), reader.GetString(2));
                var targetScope = reader.IsDBNull(3)
                    ? null
                    : new ResourceScopeReference(group.Tenant, group.Application, reader.GetGuid(3));
                grants.Add(new GroupCapabilityGrant(pattern, targetScope, reader.GetBoolean(4)));
            }

            return grants.AsReadOnly();
        }
    }
}
