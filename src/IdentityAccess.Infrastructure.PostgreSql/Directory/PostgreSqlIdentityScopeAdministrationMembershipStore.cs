using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Persists identity-scope administration group memberships in PostgreSQL.</summary>
    internal sealed class PostgreSqlIdentityScopeAdministrationMembershipStore(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IIdentityScopeAdministrationMembershipStore
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<IdentityScopeAdministrationGroupMembership>> ListAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group,
            CancellationToken cancellationToken)
        {
            Ensure(route, group);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT user_id
                FROM identity_access.identity_scope_administration_group_memberships
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND group_id = @group_id
                ORDER BY user_id;
                """, connection);

            AddGroup(command, group);
            var result = new List<IdentityScopeAdministrationGroupMembership>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                result.Add(
                    new IdentityScopeAdministrationGroupMembership(
                        group,
                        new SubjectReference(group.IdentityScopeId, reader.GetGuid(0))));
            }

            return result.AsReadOnly();
        }

        /// <inheritdoc />
        public async Task<IdentityScopeAdministrationGroupMembership?> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupMembership membership,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membership);
            Ensure(route, membership.Group);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                WITH eligible AS
                (
                    SELECT 1
                    FROM identity_access.identity_scope_administration_groups AS g
                    INNER JOIN identity_access.users AS u
                      ON u.identity_scope_id = g.identity_scope_id
                     AND u.user_id = @user_id
                    WHERE g.identity_scope_id = @scope
                      AND g.application_key = @application_key
                      AND g.group_id = @group_id
                      AND g.status = @active_group_status
                      AND u.status = @active_user_status
                )
                INSERT INTO identity_access.identity_scope_administration_group_memberships
                    (identity_scope_id, application_key, group_id, user_id)
                SELECT @scope, @application_key, @group_id, @user_id
                FROM eligible
                RETURNING 1;
                """, connection);

            AddGroup(command, membership.Group);
            command.Parameters.AddWithValue("user_id", membership.Subject.UserId);
            command.Parameters.AddWithValue("active_group_status", (short)GroupStatus.Active);
            command.Parameters.AddWithValue("active_user_status", (short)UserStatus.Active);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is null or DBNull ? null : membership;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupMembership membership,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membership);
            Ensure(route, membership.Group);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.identity_scope_administration_group_memberships
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND group_id = @group_id
                  AND user_id = @user_id;
                """, connection);

            AddGroup(command, membership.Group);
            command.Parameters.AddWithValue("user_id", membership.Subject.UserId);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }

        private static void Ensure(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, group.IdentityScopeId);
            if (route.Request.Application != group.Application)
                throw new InvalidOperationException("Administration group application does not match the route.");
        }

        private static void AddGroup(
            NpgsqlCommand command,
            IdentityScopeAdministrationGroupReference group)
        {
            command.Parameters.AddWithValue("scope", group.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);
        }
    }
}
