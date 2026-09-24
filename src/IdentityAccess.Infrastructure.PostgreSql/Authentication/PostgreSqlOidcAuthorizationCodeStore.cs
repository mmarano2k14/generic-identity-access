using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Authentication
{
    /// <summary>
    /// Persists SHA-256 authorization-code hashes and atomically consumes codes with PKCE, active
    /// session, and current active-user checks.
    /// </summary>
    internal sealed class PostgreSqlOidcAuthorizationCodeStore(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IOidcAuthorizationCodeStore
    {
        /// <inheritdoc />
        public async Task<bool> CreateForActiveSessionAsync(
            ResolvedDatabaseRoute route,
            OidcAuthorizationCodeGrant grant,
            byte[] codeHash,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(grant);
            ArgumentNullException.ThrowIfNull(codeHash);

            EnsureRoute(
                route,
                grant.Subject.IdentityScopeId,
                grant.Application);

            if (codeHash.Length != 32)
            {
                throw new ArgumentException(
                    "A SHA-256 authorization-code hash is required.",
                    nameof(codeHash));
            }

            await using var connection =
                (NpgsqlConnection)await connectionFactory
                    .OpenAsync(
                        route,
                        cancellationToken)
                    .ConfigureAwait(false);

            await using var command =
                new NpgsqlCommand(
                    """
                    INSERT INTO identity_access.oidc_authorization_codes
                    (
                        identity_scope_id,
                        code_id,
                        code_hash,
                        user_id,
                        session_id,
                        client_id,
                        application_key,
                        authentication_context_key,
                        redirect_uri,
                        scope,
                        code_challenge,
                        code_challenge_method,
                        nonce,
                        authenticated_at,
                        assurance_level,
                        assurance_methods,
                        issued_at,
                        expires_at
                    )
                    SELECT
                        @scope_id,
                        @code_id,
                        @code_hash,
                        @user_id,
                        @session_id,
                        @client_id,
                        @application_key,
                        @authentication_context_key,
                        @redirect_uri,
                        @scope,
                        @code_challenge,
                        'S256',
                        @nonce,
                        @authenticated_at,
                        @assurance_level,
                        @assurance_methods,
                        @issued_at,
                        @expires_at
                    FROM identity_access.user_sessions AS s
                    INNER JOIN identity_access.users AS u
                      ON u.identity_scope_id = s.identity_scope_id
                     AND u.user_id = s.user_id
                    WHERE s.identity_scope_id = @scope_id
                      AND s.session_id = @session_id
                      AND s.user_id = @user_id
                      AND s.client_id = @client_id
                      AND s.application_key = @application_key
                      AND s.authentication_context_key = @authentication_context_key
                      AND s.revoked_at IS NULL
                      AND s.expires_at > @issued_at
                      AND u.status = @active_user_status
                    RETURNING 1;
                    """,
                    connection);

            command.Parameters.AddWithValue(
                "scope_id",
                grant.Subject.IdentityScopeId);

            command.Parameters.AddWithValue(
                "code_id",
                grant.CodeId);

            command.Parameters.AddWithValue(
                "code_hash",
                codeHash);

            command.Parameters.AddWithValue(
                "user_id",
                grant.Subject.UserId);

            command.Parameters.AddWithValue(
                "session_id",
                grant.SessionId);

            command.Parameters.AddWithValue(
                "client_id",
                grant.ClientId);

            command.Parameters.AddWithValue(
                "application_key",
                grant.Application.Value);

            command.Parameters.AddWithValue(
                "authentication_context_key",
                grant.AuthenticationContextKey);

            command.Parameters.AddWithValue(
                "redirect_uri",
                grant.RedirectUri);

            command.Parameters.AddWithValue(
                "scope",
                grant.Scope);

            command.Parameters.AddWithValue(
                "code_challenge",
                grant.CodeChallenge);

            command.Parameters.AddWithValue(
                "nonce",
                grant.Nonce);

            command.Parameters.AddWithValue(
                "authenticated_at",
                grant.AuthenticatedAt);

            command.Parameters.AddWithValue(
                "assurance_level",
                (short)grant.Assurance.Level);

            command.Parameters.AddWithValue(
                "assurance_methods",
                NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text,
                grant.Assurance.Methods.ToArray());

            command.Parameters.AddWithValue(
                "issued_at",
                grant.IssuedAt);

            command.Parameters.AddWithValue(
                "expires_at",
                grant.ExpiresAt);

            command.Parameters.AddWithValue(
                "active_user_status",
                (short)UserStatus.Active);

            var result =
                await command
                    .ExecuteScalarAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            return result is not null and not DBNull;
        }

        /// <inheritdoc />
        public async Task<OidcAuthorizationCodeGrant?> ConsumeAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            string redirectUri,
            byte[] codeHash,
            string expectedCodeChallenge,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
            ArgumentNullException.ThrowIfNull(codeHash);
            ArgumentException.ThrowIfNullOrWhiteSpace(expectedCodeChallenge);

            if (codeHash.Length != 32)
            {
                return null;
            }

            await using var connection =
                (NpgsqlConnection)await connectionFactory
                    .OpenAsync(
                        route,
                        cancellationToken)
                    .ConfigureAwait(false);

            await using var command =
                new NpgsqlCommand(
                    """
                    UPDATE identity_access.oidc_authorization_codes AS c
                    SET consumed_at = @now
                    FROM identity_access.user_sessions AS s
                    INNER JOIN identity_access.users AS u
                      ON u.identity_scope_id = s.identity_scope_id
                     AND u.user_id = s.user_id
                    WHERE c.identity_scope_id = @scope_id
                      AND c.code_hash = @code_hash
                      AND c.client_id = @client_id
                      AND c.redirect_uri = @redirect_uri
                      AND c.code_challenge = @code_challenge
                      AND c.code_challenge_method = 'S256'
                      AND c.application_key = @application_key
                      AND c.consumed_at IS NULL
                      AND c.expires_at > @now
                      AND s.identity_scope_id = c.identity_scope_id
                      AND s.session_id = c.session_id
                      AND s.user_id = c.user_id
                      AND s.client_id = c.client_id
                      AND s.application_key = c.application_key
                      AND s.authentication_context_key = c.authentication_context_key
                      AND s.revoked_at IS NULL
                      AND s.expires_at > @now
                      AND u.status = @active_user_status
                    RETURNING
                        c.code_id,
                        c.user_id,
                        c.session_id,
                        c.client_id,
                        c.application_key,
                        c.authentication_context_key,
                        c.redirect_uri,
                        c.scope,
                        c.code_challenge,
                        c.nonce,
                        c.authenticated_at,
                        c.assurance_level,
                        c.assurance_methods,
                        c.issued_at,
                        c.expires_at,
                        c.consumed_at;
                    """,
                    connection);

            command.Parameters.AddWithValue(
                "scope_id",
                route.Request.IdentityScopeId);

            command.Parameters.AddWithValue(
                "code_hash",
                codeHash);

            command.Parameters.AddWithValue(
                "client_id",
                clientId);

            command.Parameters.AddWithValue(
                "redirect_uri",
                redirectUri);

            command.Parameters.AddWithValue(
                "code_challenge",
                expectedCodeChallenge);

            command.Parameters.AddWithValue(
                "application_key",
                route.Request.Application.Value);

            command.Parameters.AddWithValue(
                "now",
                now);

            command.Parameters.AddWithValue(
                "active_user_status",
                (short)UserStatus.Active);

            await using var reader =
                await command
                    .ExecuteReaderAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!await reader
                .ReadAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                return null;
            }

            return new OidcAuthorizationCodeGrant(
                reader.GetGuid(0),
                new SubjectReference(
                    route.Request.IdentityScopeId,
                    reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetString(3),
                new ApplicationKey(
                    reader.GetString(4)),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetFieldValue<DateTimeOffset>(10),
                reader.GetFieldValue<DateTimeOffset>(13),
                reader.GetFieldValue<DateTimeOffset>(14),
                reader.GetFieldValue<DateTimeOffset>(15),
                new AuthenticationAssurance(
                    (AuthenticationAssuranceLevel)reader.GetInt16(11),
                    reader.GetFieldValue<string[]>(12),
                    reader.GetFieldValue<DateTimeOffset>(10)));
        }

        private static void EnsureRoute(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(application);

            PostgreSqlDirectoryGuard.EnsureScope(
                route,
                identityScopeId);

            if (route.Request.Application != application)
            {
                throw new InvalidOperationException(
                    "OIDC authorization-code application does not match the resolved route.");
            }
        }
    }
}
