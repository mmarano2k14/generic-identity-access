using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Authentication
{
    /// <summary>
    /// Implements atomic PostgreSQL credential mutations and password-change session revocation.
    /// </summary>
    internal sealed class PostgreSqlCredentialMutationStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : ICredentialMutationStore
    {
        /// <inheritdoc />
        public async Task<VersionedRecord<PasswordCredential>> CreateForExistingUserAsync(
            ResolvedDatabaseRoute route,
            PasswordCredential credential,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(credential);
            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                credential.Subject.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                WITH inserted AS
                (
                    INSERT INTO identity_access.password_credentials
                        (identity_scope_id, user_id, login_identifier,
                         normalized_login_identifier, password_hash)
                    SELECT
                        @scope,
                        @user_id,
                        @login,
                        @normalized_login,
                        @password_hash
                    FROM identity_access.users AS u
                    WHERE u.identity_scope_id = @scope
                      AND u.user_id = @user_id
                    RETURNING row_version
                )
                SELECT row_version
                FROM inserted;
                """, connection);

            AddCredentialParameters(command, credential);

            var result = await command
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is null or DBNull)
            {
                throw new InvalidOperationException(
                    "The credential subject does not exist.");
            }

            return new VersionedRecord<PasswordCredential>(
                credential,
                (long)result);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<PasswordCredential>> UpdatePasswordAndRevokeSessionsAsync(
            ResolvedDatabaseRoute route,
            PasswordCredential credential,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(credential);
            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                credential.Subject.IdentityScopeId);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                WITH subject AS
                (
                    SELECT 1
                    FROM identity_access.users AS u
                    WHERE u.identity_scope_id = @scope
                      AND u.user_id = @user_id
                ),
                updated AS
                (
                    UPDATE identity_access.password_credentials
                    SET login_identifier = @login,
                        normalized_login_identifier = @normalized_login,
                        password_hash = @password_hash,
                        failed_access_count = 0,
                        lockout_until = NULL,
                        row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND row_version = @expected_version
                      AND EXISTS (SELECT 1 FROM subject)
                    RETURNING row_version
                ),
                revoked AS
                (
                    UPDATE identity_access.user_sessions
                    SET revoked_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND revoked_at IS NULL
                      AND EXISTS (SELECT 1 FROM updated)
                    RETURNING 1
                ),
                refresh_revoked AS
                (
                    UPDATE identity_access.oidc_refresh_tokens
                    SET revoked_at = transaction_timestamp(),
                        revocation_reason = 'credential_changed'
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND revoked_at IS NULL
                      AND EXISTS (SELECT 1 FROM updated)
                    RETURNING 1
                )
                SELECT
                    EXISTS (SELECT 1 FROM subject),
                    (SELECT row_version FROM updated LIMIT 1),
                    (SELECT count(*) FROM revoked),
                    (SELECT count(*) FROM refresh_revoked);
                """, connection);

            AddCredentialParameters(command, credential);
            command.Parameters.AddWithValue(
                "expected_version",
                expectedVersion);

            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    "Credential mutation did not return a result.");
            }

            if (!reader.GetBoolean(0))
            {
                throw new InvalidOperationException(
                    "The credential subject does not exist.");
            }

            if (reader.IsDBNull(1))
            {
                throw new IdentityConcurrencyException();
            }

            return new VersionedRecord<PasswordCredential>(
                credential,
                reader.GetInt64(1));
        }

        private static void AddCredentialParameters(
            NpgsqlCommand command,
            PasswordCredential credential)
        {
            command.Parameters.AddWithValue(
                "scope",
                credential.Subject.IdentityScopeId);
            command.Parameters.AddWithValue(
                "user_id",
                credential.Subject.UserId);
            command.Parameters.AddWithValue(
                "login",
                credential.LoginIdentifier.Value);
            command.Parameters.AddWithValue(
                "normalized_login",
                credential.LoginIdentifier.NormalizedValue);
            command.Parameters.AddWithValue(
                "password_hash",
                credential.PasswordHash);
        }
    }
}
