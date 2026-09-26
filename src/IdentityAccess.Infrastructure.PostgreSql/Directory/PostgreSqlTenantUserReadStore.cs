using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Reads tenant users through membership-constrained PostgreSQL joins.</summary>
    internal sealed class PostgreSqlTenantUserReadStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : ITenantUserReadStore
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<TenantUserReadRecord>> ListAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            string? search,
            bool activeMembershipsOnly,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                SELECT memberships.membership_id,
                       memberships.user_id,
                       users.display_name,
                       users.status,
                       memberships.status,
                       users.row_version,
                       memberships.row_version
                FROM identity_access.tenant_memberships AS memberships
                INNER JOIN identity_access.users AS users
                    ON users.identity_scope_id = memberships.identity_scope_id
                   AND users.user_id = memberships.user_id
                WHERE memberships.identity_scope_id = @scope
                  AND memberships.tenant_id = @tenant_id
                  AND (@active_memberships_only = FALSE OR memberships.status = @active_membership_status)
                  AND (@search_pattern IS NULL
                       OR lower(users.display_name) LIKE @search_pattern
                       OR memberships.membership_id = @search_id
                       OR memberships.user_id = @search_id)
                ORDER BY lower(users.display_name), memberships.user_id, memberships.membership_id
                OFFSET @offset
                LIMIT @limit;
                """, connection);

            command.Parameters.AddWithValue("scope", tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.TenantId);
            command.Parameters.AddWithValue("active_memberships_only", activeMembershipsOnly);
            command.Parameters.AddWithValue("active_membership_status", (short)MembershipStatus.Active);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("offset", offset);
            command.Parameters.AddWithValue("limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var records = new List<TenantUserReadRecord>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var subject = new SubjectReference(tenant.IdentityScopeId, reader.GetGuid(1));
                records.Add(new TenantUserReadRecord(
                    reader.GetGuid(0),
                    tenant,
                    subject,
                    reader.GetString(2),
                    (UserStatus)reader.GetInt16(3),
                    (MembershipStatus)reader.GetInt16(4),
                    reader.GetInt64(5),
                    reader.GetInt64(6)));
            }

            return records;
        }
    }
}
