using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>
    /// User persistence with explicit identity-scope predicates, optimistic row-version checks,
    /// and atomic session revocation when an account becomes inactive.
    /// </summary>
    internal sealed class PostgreSqlUserDirectoryStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IUserDirectoryStore
    {
        /// <inheritdoc />
        public async Task<VersionedRecord<User>?> GetAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                subject.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                SELECT display_name, status, row_version
                FROM identity_access.users
                WHERE identity_scope_id = @scope
                  AND user_id = @user_id;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                subject.IdentityScopeId);
            command.Parameters.AddWithValue(
                "user_id",
                subject.UserId);

            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            var user = new User(
                subject,
                reader.GetString(0),
                (UserStatus)reader.GetInt16(1));

            return new VersionedRecord<User>(
                user,
                reader.GetInt64(2));
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<User>> CreateAsync(
            ResolvedDatabaseRoute route,
            User user,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user);
            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                user.Subject.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.users
                    (identity_scope_id, user_id, display_name, status)
                VALUES
                    (@scope, @user_id, @display_name, @status)
                RETURNING row_version;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                user.Subject.IdentityScopeId);
            command.Parameters.AddWithValue(
                "user_id",
                user.Subject.UserId);
            command.Parameters.AddWithValue(
                "display_name",
                user.DisplayName);
            command.Parameters.AddWithValue(
                "status",
                (short)user.Status);

            var version = (long)(
                await command.ExecuteScalarAsync(cancellationToken)
                    .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "The inserted user did not return a row version."));

            return new VersionedRecord<User>(
                user,
                version);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<User>> UpdateAsync(
            ResolvedDatabaseRoute route,
            User user,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                user.Subject.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                WITH updated AS
                (
                    UPDATE identity_access.users
                    SET display_name = @display_name,
                        status = @status,
                        row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND row_version = @expected_version
                    RETURNING row_version
                ),
                revoked AS
                (
                    UPDATE identity_access.user_sessions
                    SET revoked_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND revoked_at IS NULL
                      AND @status <> @active_user_status
                      AND EXISTS (SELECT 1 FROM updated)
                    RETURNING 1
                )
                SELECT row_version
                FROM updated;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                user.Subject.IdentityScopeId);
            command.Parameters.AddWithValue(
                "user_id",
                user.Subject.UserId);
            command.Parameters.AddWithValue(
                "display_name",
                user.DisplayName);
            command.Parameters.AddWithValue(
                "status",
                (short)user.Status);
            command.Parameters.AddWithValue(
                "active_user_status",
                (short)UserStatus.Active);
            command.Parameters.AddWithValue(
                "expected_version",
                expectedVersion);

            var result = await command
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is null or DBNull)
                throw new IdentityConcurrencyException();

            return new VersionedRecord<User>(
                user,
                (long)result);
        }
    }
}
