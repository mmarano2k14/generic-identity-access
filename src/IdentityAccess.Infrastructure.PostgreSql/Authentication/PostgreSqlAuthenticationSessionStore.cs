using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Authentication
{
    /// <summary>
    /// Persists and validates local authentication sessions using current user state as part of
    /// the session lifecycle contract.
    /// </summary>
    internal sealed class PostgreSqlAuthenticationSessionStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IAuthenticationSessionStore
    {
        /// <inheritdoc />
        public async Task<bool> CreateForActiveUserAsync(
            ResolvedDatabaseRoute route,
            AuthenticationSession session,
            byte[] tokenHash,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(tokenHash);
            PostgreSqlDirectoryGuard.EnsureScope(route, session.Subject.IdentityScopeId);

            if (tokenHash.Length != 32)
                throw new ArgumentException(
                    "A SHA-256 session token hash is required.",
                    nameof(tokenHash));

            if (route.Request.Application != session.Application)
                throw new InvalidOperationException(
                    "The session application does not match the resolved route.");

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.user_sessions
                    (identity_scope_id, session_id, user_id, client_id, application_key,
                     authentication_context_key, token_hash, created_at, expires_at)
                SELECT
                    @scope, @session_id, @user_id, @client_id, @application_key,
                    @context_key, @token_hash, @created_at, @expires_at
                FROM identity_access.users AS u
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.status = @active_user_status
                RETURNING 1;
                """, connection);

            AddSessionParameters(command, session, tokenHash);
            command.Parameters.AddWithValue(
                "active_user_status",
                (short)UserStatus.Active);

            var result = await command
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return result is not null and not DBNull;
        }

        /// <inheritdoc />
        public async Task<AuthenticationSession?> ValidateAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            byte[] tokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

            if (sessionId == Guid.Empty)
                return null;

            ArgumentNullException.ThrowIfNull(tokenHash);
            if (tokenHash.Length != 32)
                return null;

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                SELECT
                    s.user_id,
                    s.application_key,
                    s.authentication_context_key,
                    s.created_at,
                    s.expires_at,
                    s.revoked_at
                FROM identity_access.user_sessions AS s
                INNER JOIN identity_access.users AS u
                    ON u.identity_scope_id = s.identity_scope_id
                   AND u.user_id = s.user_id
                WHERE s.identity_scope_id = @scope
                  AND s.session_id = @session_id
                  AND s.client_id = @client_id
                  AND s.token_hash = @token_hash
                  AND s.revoked_at IS NULL
                  AND s.expires_at > @now
                  AND u.status = @active_user_status;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                route.Request.IdentityScopeId);
            command.Parameters.AddWithValue("session_id", sessionId);
            command.Parameters.AddWithValue("client_id", clientId);
            command.Parameters.AddWithValue("token_hash", tokenHash);
            command.Parameters.AddWithValue("now", now);
            command.Parameters.AddWithValue(
                "active_user_status",
                (short)UserStatus.Active);

            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            return new AuthenticationSession(
                sessionId,
                new SubjectReference(
                    route.Request.IdentityScopeId,
                    reader.GetGuid(0)),
                clientId,
                new ApplicationKey(reader.GetString(1)),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.IsDBNull(5)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(5));
        }

        /// <inheritdoc />
        public async Task<bool> RevokeAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            byte[] tokenHash,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

            if (sessionId == Guid.Empty)
                return false;

            ArgumentNullException.ThrowIfNull(tokenHash);
            if (tokenHash.Length != 32)
                return false;

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.user_sessions
                SET revoked_at = @revoked_at
                WHERE identity_scope_id = @scope
                  AND session_id = @session_id
                  AND client_id = @client_id
                  AND token_hash = @token_hash
                  AND revoked_at IS NULL;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                route.Request.IdentityScopeId);
            command.Parameters.AddWithValue("session_id", sessionId);
            command.Parameters.AddWithValue("client_id", clientId);
            command.Parameters.AddWithValue("token_hash", tokenHash);
            command.Parameters.AddWithValue("revoked_at", revokedAt);

            return await command
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false) == 1;
        }

        /// <inheritdoc />
        public async Task<int> RevokeAllForSubjectAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                subject.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.user_sessions
                SET revoked_at = @revoked_at
                WHERE identity_scope_id = @scope
                  AND user_id = @user_id
                  AND revoked_at IS NULL
                  AND expires_at > @revoked_at;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                subject.IdentityScopeId);
            command.Parameters.AddWithValue(
                "user_id",
                subject.UserId);
            command.Parameters.AddWithValue(
                "revoked_at",
                revokedAt);

            return await command
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> RevokeAllForClientAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.user_sessions
                SET revoked_at = @revoked_at
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND client_id = @client_id
                  AND revoked_at IS NULL
                  AND expires_at > @revoked_at;
                """, connection);

            command.Parameters.AddWithValue(
                "scope",
                route.Request.IdentityScopeId);
            command.Parameters.AddWithValue(
                "application_key",
                route.Request.Application.Value);
            command.Parameters.AddWithValue(
                "client_id",
                clientId);
            command.Parameters.AddWithValue(
                "revoked_at",
                revokedAt);

            return await command
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private static void AddSessionParameters(
            NpgsqlCommand command,
            AuthenticationSession session,
            byte[] tokenHash)
        {
            command.Parameters.AddWithValue(
                "scope",
                session.Subject.IdentityScopeId);
            command.Parameters.AddWithValue(
                "session_id",
                session.SessionId);
            command.Parameters.AddWithValue(
                "user_id",
                session.Subject.UserId);
            command.Parameters.AddWithValue(
                "client_id",
                session.ClientId);
            command.Parameters.AddWithValue(
                "application_key",
                session.Application.Value);
            command.Parameters.AddWithValue(
                "context_key",
                session.AuthenticationContextKey);
            command.Parameters.AddWithValue(
                "token_hash",
                tokenHash);
            command.Parameters.AddWithValue(
                "created_at",
                session.CreatedAt);
            command.Parameters.AddWithValue(
                "expires_at",
                session.ExpiresAt);
        }
    }
}
