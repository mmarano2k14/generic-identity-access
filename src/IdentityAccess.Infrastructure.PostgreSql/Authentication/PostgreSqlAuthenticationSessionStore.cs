using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;
using NpgsqlTypes;

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
                     authentication_context_key, token_hash, created_at, expires_at,
                     assurance_level, assurance_methods, assurance_verified_at)
                SELECT
                    @scope, @session_id, @user_id, @client_id, @application_key,
                    @context_key, @token_hash, @created_at, @expires_at,
                    @assurance_level, @assurance_methods, @assurance_verified_at
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
                    s.revoked_at,
                    s.assurance_level,
                    s.assurance_methods,
                    s.assurance_verified_at
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

            command.Parameters.AddWithValue("scope", route.Request.IdentityScopeId);
            command.Parameters.AddWithValue("session_id", sessionId);
            command.Parameters.AddWithValue("client_id", clientId);
            command.Parameters.AddWithValue("token_hash", tokenHash);
            command.Parameters.AddWithValue("now", now);
            command.Parameters.AddWithValue("active_user_status", (short)UserStatus.Active);

            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            return ReadSession(reader, route.Request.IdentityScopeId, sessionId, clientId);
        }

        /// <inheritdoc />
        public async Task<AuthenticationSession?> ValidateReferenceAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

            if (sessionId == Guid.Empty)
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
                    s.revoked_at,
                    s.assurance_level,
                    s.assurance_methods,
                    s.assurance_verified_at
                FROM identity_access.user_sessions AS s
                INNER JOIN identity_access.users AS u
                    ON u.identity_scope_id = s.identity_scope_id
                   AND u.user_id = s.user_id
                WHERE s.identity_scope_id = @scope
                  AND s.session_id = @session_id
                  AND s.client_id = @client_id
                  AND s.application_key = @application_key
                  AND s.revoked_at IS NULL
                  AND s.expires_at > @now
                  AND u.status = @active_user_status;
                """, connection);

            command.Parameters.AddWithValue("scope", route.Request.IdentityScopeId);
            command.Parameters.AddWithValue("session_id", sessionId);
            command.Parameters.AddWithValue("client_id", clientId);
            command.Parameters.AddWithValue("application_key", route.Request.Application.Value);
            command.Parameters.AddWithValue("now", now);
            command.Parameters.AddWithValue("active_user_status", (short)UserStatus.Active);

            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            return ReadSession(reader, route.Request.IdentityScopeId, sessionId, clientId);
        }

        /// <inheritdoc />
        public async Task<AuthenticationSession?> UpgradeAssuranceAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            string factorMethodReference,
            DateTimeOffset verifiedAt,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(subject);
            PostgreSqlDirectoryGuard.EnsureScope(route, subject.IdentityScopeId);
            if (sessionId == Guid.Empty)
                throw new ArgumentException("A session identifier is required.", nameof(sessionId));
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationContextKey);
            var factor = AuthenticationMethodReferences.ValidateFactor(factorMethodReference);

            if (route.Request.Application != application)
                throw new InvalidOperationException("The session application does not match the resolved route.");

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await connection
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            AuthenticationSession? current;

            await using (var inspect = new NpgsqlCommand("""
                SELECT
                    s.user_id,
                    s.application_key,
                    s.authentication_context_key,
                    s.created_at,
                    s.expires_at,
                    s.revoked_at,
                    s.assurance_level,
                    s.assurance_methods,
                    s.assurance_verified_at
                FROM identity_access.user_sessions AS s
                INNER JOIN identity_access.users AS u
                    ON u.identity_scope_id = s.identity_scope_id
                   AND u.user_id = s.user_id
                WHERE s.identity_scope_id = @scope
                  AND s.session_id = @session_id
                  AND s.user_id = @user_id
                  AND s.client_id = @client_id
                  AND s.application_key = @application_key
                  AND s.authentication_context_key = @context_key
                  AND s.revoked_at IS NULL
                  AND s.expires_at > @verified_at
                  AND u.status = @active_user_status
                FOR UPDATE OF s;
                """, connection, transaction))
            {
                inspect.Parameters.AddWithValue("scope", subject.IdentityScopeId);
                inspect.Parameters.AddWithValue("session_id", sessionId);
                inspect.Parameters.AddWithValue("user_id", subject.UserId);
                inspect.Parameters.AddWithValue("client_id", clientId);
                inspect.Parameters.AddWithValue("application_key", application.Value);
                inspect.Parameters.AddWithValue("context_key", authenticationContextKey);
                inspect.Parameters.AddWithValue("verified_at", verifiedAt);
                inspect.Parameters.AddWithValue("active_user_status", (short)UserStatus.Active);

                await using var reader = await inspect
                    .ExecuteReaderAsync(cancellationToken)
                    .ConfigureAwait(false);

                current = await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                    ? ReadSession(reader, subject.IdentityScopeId, sessionId, clientId)
                    : null;
            }

            if (current is null)
                return null;

            var assuranceTime = verifiedAt < current.Assurance.VerifiedAt
                ? current.Assurance.VerifiedAt
                : verifiedAt;
            var upgraded = current.Assurance.WithFactor(factor, assuranceTime);

            await using (var update = new NpgsqlCommand("""
                UPDATE identity_access.user_sessions
                SET assurance_level = @assurance_level,
                    assurance_methods = @assurance_methods,
                    assurance_verified_at = @assurance_verified_at
                WHERE identity_scope_id = @scope
                  AND session_id = @session_id
                  AND revoked_at IS NULL;
                """, connection, transaction))
            {
                update.Parameters.AddWithValue("scope", subject.IdentityScopeId);
                update.Parameters.AddWithValue("session_id", sessionId);
                AddAssuranceParameters(update, upgraded);

                if (await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                    throw new InvalidOperationException("Session assurance state changed while its row lock was held.");
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new AuthenticationSession(
                current.SessionId,
                current.Subject,
                current.ClientId,
                current.Application,
                current.AuthenticationContextKey,
                current.CreatedAt,
                current.ExpiresAt,
                current.RevokedAt,
                upgraded);
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

            command.Parameters.AddWithValue("scope", route.Request.IdentityScopeId);
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
            PostgreSqlDirectoryGuard.EnsureScope(route, subject.IdentityScopeId);

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

            command.Parameters.AddWithValue("scope", subject.IdentityScopeId);
            command.Parameters.AddWithValue("user_id", subject.UserId);
            command.Parameters.AddWithValue("revoked_at", revokedAt);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

            command.Parameters.AddWithValue("scope", route.Request.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", route.Request.Application.Value);
            command.Parameters.AddWithValue("client_id", clientId);
            command.Parameters.AddWithValue("revoked_at", revokedAt);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static AuthenticationSession ReadSession(
            NpgsqlDataReader reader,
            Guid identityScopeId,
            Guid sessionId,
            string clientId)
        {
            var createdAt = reader.GetFieldValue<DateTimeOffset>(3);
            var assurance = new AuthenticationAssurance(
                (AuthenticationAssuranceLevel)reader.GetInt16(6),
                reader.GetFieldValue<string[]>(7),
                reader.GetFieldValue<DateTimeOffset>(8));

            return new AuthenticationSession(
                sessionId,
                new SubjectReference(identityScopeId, reader.GetGuid(0)),
                clientId,
                new ApplicationKey(reader.GetString(1)),
                reader.GetString(2),
                createdAt,
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.IsDBNull(5)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(5),
                assurance);
        }

        private static void AddSessionParameters(
            NpgsqlCommand command,
            AuthenticationSession session,
            byte[] tokenHash)
        {
            command.Parameters.AddWithValue("scope", session.Subject.IdentityScopeId);
            command.Parameters.AddWithValue("session_id", session.SessionId);
            command.Parameters.AddWithValue("user_id", session.Subject.UserId);
            command.Parameters.AddWithValue("client_id", session.ClientId);
            command.Parameters.AddWithValue("application_key", session.Application.Value);
            command.Parameters.AddWithValue("context_key", session.AuthenticationContextKey);
            command.Parameters.AddWithValue("token_hash", tokenHash);
            command.Parameters.AddWithValue("created_at", session.CreatedAt);
            command.Parameters.AddWithValue("expires_at", session.ExpiresAt);
            AddAssuranceParameters(command, session.Assurance);
        }

        private static void AddAssuranceParameters(
            NpgsqlCommand command,
            AuthenticationAssurance assurance)
        {
            command.Parameters.AddWithValue("assurance_level", (short)assurance.Level);
            command.Parameters.AddWithValue(
                "assurance_methods",
                NpgsqlDbType.Array | NpgsqlDbType.Text,
                assurance.Methods.ToArray());
            command.Parameters.AddWithValue("assurance_verified_at", assurance.VerifiedAt);
        }
    }
}
