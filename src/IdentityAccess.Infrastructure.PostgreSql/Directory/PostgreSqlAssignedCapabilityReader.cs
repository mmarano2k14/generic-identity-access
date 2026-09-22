using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>
    /// Operation-scoped assignment projection. Tenant-wide bindings always apply. Scoped bindings apply to an exact
    /// target, while ancestor bindings require IncludeDescendants. RBAC wildcard evaluation remains external.
    /// </summary>
    internal sealed class PostgreSqlAssignedCapabilityReader(IIdentityDatabaseConnectionFactory connectionFactory)
        : IAssignedCapabilityReader
    {
        /// <summary>Lists effective capability grants for the subject after tenant, group, policy, and resource-scope filtering.</summary>
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
                )
                SELECT
                    ug.group_id,
                    pp.policy_id,
                    ps.statement_id,
                    ps.model_version,
                    ps.capability_resource,
                    ps.capability_feature,
                    ps.capability_action,
                    gpb.resource_scope_id,
                    gpb.include_descendants
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
                JOIN identity_access.group_policy_bindings gpb
                  ON gpb.identity_scope_id = ug.identity_scope_id
                 AND gpb.tenant_id = ug.tenant_id
                 AND gpb.application_key = ug.application_key
                 AND gpb.group_id = ug.group_id
                JOIN identity_access.permission_policies pp
                  ON pp.identity_scope_id = gpb.identity_scope_id
                 AND pp.tenant_id = gpb.tenant_id
                 AND pp.application_key = gpb.application_key
                 AND pp.policy_id = gpb.policy_id
                JOIN identity_access.policy_statements ps
                  ON ps.identity_scope_id = pp.identity_scope_id
                 AND ps.tenant_id = pp.tenant_id
                 AND ps.application_key = pp.application_key
                 AND ps.policy_id = pp.policy_id
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.status = 1
                  AND t.status = 1
                  AND tm.status = 1
                  AND ug.status = 1
                  AND pp.status = 1
                  AND (
                        (@resource_scope_id IS NULL AND gpb.resource_scope_id IS NULL)
                        OR
                        (@resource_scope_id IS NOT NULL
                         AND EXISTS (SELECT 1 FROM scope_ancestry a WHERE a.resource_scope_id = @resource_scope_id)
                         AND (
                             gpb.resource_scope_id IS NULL
                             OR gpb.resource_scope_id = @resource_scope_id
                             OR (gpb.include_descendants = TRUE AND EXISTS (
                                 SELECT 1 FROM scope_ancestry a WHERE a.resource_scope_id = gpb.resource_scope_id))
                         ))
                      )
                ORDER BY ug.group_id, pp.policy_id, ps.statement_id, gpb.resource_scope_id NULLS FIRST;
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
                var policy = new PermissionPolicyReference(tenant, application, reader.GetGuid(1));
                var statementId = reader.GetGuid(2);
                var model = new ApplicationSecurityModelReference(tenant.IdentityScopeId, application, reader.GetInt32(3));
                var pattern = new CapabilityPattern(reader.GetString(4), reader.GetString(5), reader.GetString(6));
                var grantScope = reader.IsDBNull(7) ? null : new ResourceScopeReference(tenant, application, reader.GetGuid(7));
                grants.Add(new AssignedCapabilityGrant(subject, tenant, application, group, policy,
                    statementId, model, pattern, grantScope, reader.GetBoolean(8)));
            }

            return grants.AsReadOnly();
        }
    }
}
