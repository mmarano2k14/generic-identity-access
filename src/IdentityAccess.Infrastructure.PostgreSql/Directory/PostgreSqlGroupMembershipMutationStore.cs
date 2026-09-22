using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>
    /// Implements atomic PostgreSQL group-membership creation using one statement and one
    /// database snapshot.
    /// </summary>
    internal sealed class PostgreSqlGroupMembershipMutationStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IGroupMembershipMutationStore
    {
        /// <inheritdoc />
        public async Task<GroupMembership?> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            GroupReference group,
            Guid tenantMembershipId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            if (tenantMembershipId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Tenant membership id must not be empty.",
                    nameof(tenantMembershipId));
            }

            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                WITH eligible AS
                (
                    SELECT tm.user_id
                    FROM identity_access.user_groups AS g
                    INNER JOIN identity_access.tenant_memberships AS tm
                        ON tm.identity_scope_id = g.identity_scope_id
                       AND tm.tenant_id = g.tenant_id
                    WHERE g.identity_scope_id = @scope
                      AND g.tenant_id = @tenant_id
                      AND g.application_key = @application_key
                      AND g.group_id = @group_id
                      AND g.status = @active_group_status
                      AND tm.membership_id = @membership_id
                      AND tm.status = @active_membership_status
                )
                INSERT INTO identity_access.group_memberships
                    (identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
                SELECT @scope, @tenant_id, @application_key, @group_id, @membership_id
                FROM eligible
                RETURNING (SELECT user_id FROM eligible LIMIT 1);
                """, connection);

            command.Parameters.AddWithValue("scope", group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);
            command.Parameters.AddWithValue("membership_id", tenantMembershipId);
            command.Parameters.AddWithValue("active_group_status", (short)GroupStatus.Active);
            command.Parameters.AddWithValue("active_membership_status", (short)MembershipStatus.Active);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull)
            {
                return null;
            }

            return GroupMembership.Restore(
                group,
                tenantMembershipId,
                new SubjectReference(group.Tenant.IdentityScopeId, (Guid)result));
        }
    }
}
