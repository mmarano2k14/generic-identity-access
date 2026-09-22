using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL group membership.</summary>
    internal sealed class PostgreSqlGroupMembershipStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IGroupMembershipStore
    {
        /// <summary>Adds a group membership record to the resolved database route.</summary>
        public async Task AddAsync(ResolvedDatabaseRoute route, GroupMembership membership,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membership);
            PostgreSqlDirectoryGuard.EnsureScope(route, membership.Group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.group_memberships
                    (identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
                VALUES (@scope, @tenant_id, @application_key, @group_id, @membership_id);
                """, connection);
            AddIdentity(command, membership.Group, membership.TenantMembershipId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Removes a group membership record from the resolved database route.</summary>
        public async Task<bool> RemoveAsync(ResolvedDatabaseRoute route, GroupReference group, Guid tenantMembershipId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            if (tenantMembershipId == Guid.Empty)
                throw new ArgumentException("Membership id must not be empty.", nameof(tenantMembershipId));
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.group_memberships
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND group_id = @group_id
                  AND tenant_membership_id = @membership_id;
                """, connection);
            AddIdentity(command, group, tenantMembershipId);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }

        /// <summary>Lists group membership records for the supplied scope.</summary>
        public async Task<IReadOnlyList<GroupMembership>> ListAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT tm.membership_id, tm.user_id, tm.status
                FROM identity_access.group_memberships gm
                JOIN identity_access.tenant_memberships tm
                  ON tm.identity_scope_id = gm.identity_scope_id
                 AND tm.tenant_id = gm.tenant_id
                 AND tm.membership_id = gm.tenant_membership_id
                WHERE gm.identity_scope_id = @scope
                  AND gm.tenant_id = @tenant_id
                  AND gm.application_key = @application_key
                  AND gm.group_id = @group_id
                ORDER BY tm.membership_id;
                """, connection);
            AddIdentity(command, group, null);
            var items = new List<GroupMembership>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                items.Add(GroupMembership.Restore(group, reader.GetGuid(0),
                    new SubjectReference(group.Tenant.IdentityScopeId, reader.GetGuid(1))));
            }
            return items.AsReadOnly();
        }

        private static void AddIdentity(NpgsqlCommand command, GroupReference group, Guid? membershipId)
        {
            command.Parameters.AddWithValue("scope", group.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", group.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);
            if (membershipId.HasValue)
                command.Parameters.AddWithValue("membership_id", membershipId.Value);
        }
    }
}
