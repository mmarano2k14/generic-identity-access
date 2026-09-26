using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>
    /// Operation-scoped managed-policy assignment projection. Tenant-wide bindings always apply. Scoped bindings apply
    /// to an exact target, while ancestor bindings require IncludeDescendants. Only published managed-policy versions
    /// participate in authorization; RBAC wildcard evaluation remains external.
    /// </summary>
    internal sealed class PostgreSqlAssignedCapabilityReader(IIdentityDatabaseConnectionFactory connectionFactory)
        : IAssignedCapabilityReader
    {
        /// <summary>Lists effective capability grants for the subject after tenant, group, published policy, and resource-scope filtering.</summary>
        public async Task<IReadOnlyList<AssignedCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            SubjectReference subject,
            ApplicationKey application,
            ResourceScopeReference? resourceScope,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(application);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);
            if (subject.IdentityScopeId != tenant.IdentityScopeId)
                throw new InvalidOperationException("The subject and tenant must belong to the same identity scope.");
            if (resourceScope is not null && (resourceScope.Tenant != tenant || resourceScope.Application != application))
                throw new InvalidOperationException("The requested resource scope must belong to the same tenant and application.");

            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                WITH RECURSIVE scope_ancestry AS
                (
                    SELECT rs.resource_scope_id, rs.parent_resource_scope_id
                    FROM identity_access.resource_scopes rs
                    WHERE rs.identity_scope_id = @scope
                      AND rs.tenant_id = @tenant_id
                      AND rs.application_key = @application_key
                      AND rs.resource_scope_id = @resource_scope_id
                      AND rs.status = 1

                    UNION ALL

                    SELECT parent.resource_scope_id, parent.parent_resource_scope_id
                    FROM identity_access.resource_scopes parent
                    JOIN scope_ancestry child
                      ON child.parent_resource_scope_id = parent.resource_scope_id
                    WHERE parent.identity_scope_id = @scope
                      AND parent.tenant_id = @tenant_id
                      AND parent.application_key = @application_key
                      AND parent.status = 1
                ),
                eligible_groups AS
                (
                    SELECT ug.group_id
                    FROM identity_access.users u
                    JOIN identity_access.tenants t
                      ON t.identity_scope_id = u.identity_scope_id
                     AND t.tenant_id = @tenant_id
                    JOIN identity_access.tenant_memberships tm
                      ON tm.identity_scope_id = u.identity_scope_id
                     AND tm.tenant_id = t.tenant_id
                     AND tm.user_id = u.user_id
                    JOIN identity_access.group_memberships gm
                      ON gm.identity_scope_id = tm.identity_scope_id
                     AND gm.tenant_id = tm.tenant_id
                     AND gm.tenant_membership_id = tm.membership_id
                     AND gm.application_key = @application_key
                    JOIN identity_access.user_groups ug
                      ON ug.identity_scope_id = gm.identity_scope_id
                     AND ug.tenant_id = gm.tenant_id
                     AND ug.application_key = gm.application_key
                     AND ug.group_id = gm.group_id
                    WHERE u.identity_scope_id = @scope
                      AND u.user_id = @user_id
                      AND u.status = 1
                      AND t.status = 1
                      AND tm.status = 1
                      AND ug.status = 1
                )
                SELECT
                    eg.group_id,
                    mp.policy_id,
                    mgb.policy_version,
                    mps.statement_id,
                    mpv.model_version,
                    mps.capability_resource,
                    mps.capability_feature,
                    mps.capability_action,
                    mgb.resource_scope_id,
                    mgb.include_descendants
                FROM eligible_groups eg
                JOIN identity_access.managed_group_policy_bindings mgb
                  ON mgb.identity_scope_id = @scope
                 AND mgb.tenant_id = @tenant_id
                 AND mgb.application_key = @application_key
                 AND mgb.group_id = eg.group_id
                JOIN identity_access.managed_policies mp
                  ON mp.identity_scope_id = mgb.identity_scope_id
                 AND mp.application_key = mgb.application_key
                 AND mp.policy_id = mgb.policy_id
                JOIN identity_access.managed_policy_versions mpv
                  ON mpv.identity_scope_id = mp.identity_scope_id
                 AND mpv.application_key = mp.application_key
                 AND mpv.policy_id = mp.policy_id
                 AND mpv.policy_version = mgb.policy_version
                JOIN identity_access.managed_policy_statements mps
                  ON mps.identity_scope_id = mpv.identity_scope_id
                 AND mps.application_key = mpv.application_key
                 AND mps.policy_id = mpv.policy_id
                 AND mps.policy_version = mpv.policy_version
                 AND mps.model_version = mpv.model_version
                WHERE mp.status = 1
                  AND mpv.published_at IS NOT NULL
                  AND
                  (
                      (@resource_scope_id IS NULL AND mgb.resource_scope_id IS NULL)
                      OR
                      (@resource_scope_id IS NOT NULL
                       AND EXISTS (SELECT 1 FROM scope_ancestry a WHERE a.resource_scope_id = @resource_scope_id)
                       AND
                       (
                           mgb.resource_scope_id IS NULL
                           OR mgb.resource_scope_id = @resource_scope_id
                           OR (mgb.include_descendants = TRUE AND EXISTS
                               (SELECT 1 FROM scope_ancestry a WHERE a.resource_scope_id = mgb.resource_scope_id))
                       ))
                  )
                ORDER BY eg.group_id, mp.policy_id, mgb.policy_version, mps.statement_id,
                         mgb.resource_scope_id NULLS FIRST;
                """, connection);

            command.Parameters.AddWithValue("scope", tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.TenantId);
            command.Parameters.AddWithValue("user_id", subject.UserId);
            command.Parameters.AddWithValue("application_key", application.Value);
            var target = command.Parameters.Add("resource_scope_id", NpgsqlDbType.Uuid);
            target.Value = (object?)resourceScope?.ResourceScopeId ?? DBNull.Value;

            var grants = new List<AssignedCapabilityGrant>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var group = new GroupReference(tenant, application, reader.GetGuid(0));
                var policyId = reader.GetGuid(1);
                var managedPolicy = new ManagedPolicyVersionReference(
                    new ManagedPolicyReference(tenant.IdentityScopeId, application, policyId),
                    reader.GetInt32(2));
                var statementId = reader.GetGuid(3);
                var model = new ApplicationSecurityModelReference(tenant.IdentityScopeId, application, reader.GetInt32(4));
                var pattern = new CapabilityPattern(reader.GetString(5), reader.GetString(6), reader.GetString(7));
                var grantScope = reader.IsDBNull(8) ? null : new ResourceScopeReference(tenant, application, reader.GetGuid(8));
                var includeDescendants = reader.GetBoolean(9);

                grants.Add(new AssignedCapabilityGrant(subject, tenant, application, group, managedPolicy,
                    statementId, model, pattern, grantScope, includeDescendants));
            }

            return grants.AsReadOnly();
        }
    }
}
