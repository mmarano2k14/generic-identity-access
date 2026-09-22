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
    /// Persists SHA-256 refresh-token hashes and serializes rotation by token family so consumed
    /// token replay revokes every family member without allowing a concurrent replacement to escape.
    /// </summary>
    internal sealed class PostgreSqlOidcRefreshTokenStore(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IOidcRefreshTokenStore
    {
        /// <inheritdoc />
        public async Task<bool> CreateFamilyForActiveSessionAsync(
            ResolvedDatabaseRoute route,
            OidcRefreshTokenGrant grant,
            byte[] tokenHash,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(grant);
            ArgumentNullException.ThrowIfNull(tokenHash);

            EnsureRoute(
                route,
                grant.Subject.IdentityScopeId,
                grant.Application);

            if (grant.SequenceNumber != 0 ||
                grant.ParentTokenId is not null)
            {
                throw new ArgumentException(
                    "Initial refresh-token family member is required.",
                    nameof(grant));
            }

            if (tokenHash.Length != 32)
            {
                throw new ArgumentException(
                    "A SHA-256 refresh-token hash is required.",
                    nameof(tokenHash));
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
                    INSERT INTO identity_access.oidc_refresh_tokens
                    (
                        identity_scope_id,
                        family_id,
                        token_id,
                        parent_token_id,
                        sequence_number,
                        token_hash,
                        user_id,
                        session_id,
                        client_id,
                        application_key,
                        authentication_context_key,
                        scope,
                        authenticated_at,
                        issued_at,
                        expires_at
                    )
                    SELECT
                        @scope_id,
                        @family_id,
                        @token_id,
                        @parent_token_id,
                        @sequence_number,
                        @token_hash,
                        @user_id,
                        @session_id,
                        @client_id,
                        @application_key,
                        @authentication_context_key,
                        @scope,
                        @authenticated_at,
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

            AddGrantParameters(
                command,
                grant,
                tokenHash);

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
        public async Task<OidcRefreshTokenRotationResult> RotateAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            byte[] presentedTokenHash,
            Guid replacementTokenId,
            byte[] replacementTokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(presentedTokenHash);
            ArgumentNullException.ThrowIfNull(replacementTokenHash);

            if (replacementTokenId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Replacement token id must not be empty.",
                    nameof(replacementTokenId));
            }

            if (presentedTokenHash.Length != 32)
            {
                return OidcRefreshTokenRotationResult.Invalid();
            }

            if (replacementTokenHash.Length != 32)
            {
                throw new ArgumentException(
                    "A SHA-256 replacement refresh-token hash is required.",
                    nameof(replacementTokenHash));
            }

            await using var connection =
                (NpgsqlConnection)await connectionFactory
                    .OpenAsync(
                        route,
                        cancellationToken)
                    .ConfigureAwait(false);

            await using var transaction =
                await connection
                    .BeginTransactionAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            Guid? familyId;

            await using (var lookup =
                new NpgsqlCommand(
                    """
                    SELECT family_id
                    FROM identity_access.oidc_refresh_tokens
                    WHERE identity_scope_id = @scope_id
                      AND token_hash = @token_hash
                      AND client_id = @client_id
                      AND application_key = @application_key
                    LIMIT 1;
                    """,
                    connection,
                    transaction))
            {
                lookup.Parameters.AddWithValue(
                    "scope_id",
                    route.Request.IdentityScopeId);
                lookup.Parameters.AddWithValue(
                    "token_hash",
                    presentedTokenHash);
                lookup.Parameters.AddWithValue(
                    "client_id",
                    clientId);
                lookup.Parameters.AddWithValue(
                    "application_key",
                    route.Request.Application.Value);

                var value =
                    await lookup
                        .ExecuteScalarAsync(
                            cancellationToken)
                        .ConfigureAwait(false);

                familyId =
                    value is Guid guid
                        ? guid
                        : null;
            }

            if (familyId is null)
            {
                return OidcRefreshTokenRotationResult.Invalid();
            }

            await using (var familyLock =
                new NpgsqlCommand(
                    """
                    SELECT pg_advisory_xact_lock(
                        hashtextextended(@family_id::text, 0));
                    """,
                    connection,
                    transaction))
            {
                familyLock.Parameters.AddWithValue(
                    "family_id",
                    familyId.Value);

                await familyLock
                    .ExecuteNonQueryAsync(
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            Guid tokenId;
            long sequenceNumber;
            Guid userId;
            Guid sessionId;
            string storedClientId;
            ApplicationKey application;
            string authenticationContextKey;
            string scope;
            DateTimeOffset authenticatedAt;
            DateTimeOffset expiresAt;
            DateTimeOffset? consumedAt;
            DateTimeOffset? revokedAt;
            bool sessionEligible;
            bool familyRevoked;

            await using (var inspect =
                new NpgsqlCommand(
                    """
                    SELECT
                        t.token_id,
                        t.sequence_number,
                        t.user_id,
                        t.session_id,
                        t.client_id,
                        t.application_key,
                        t.authentication_context_key,
                        t.scope,
                        t.authenticated_at,
                        t.expires_at,
                        t.consumed_at,
                        t.revoked_at,
                        (
                            s.revoked_at IS NULL
                            AND s.expires_at > @now
                            AND s.client_id = t.client_id
                            AND s.application_key = t.application_key
                            AND s.authentication_context_key = t.authentication_context_key
                            AND u.status = @active_user_status
                        ) AS session_eligible,
                        EXISTS
                        (
                            SELECT 1
                            FROM identity_access.oidc_refresh_tokens AS family
                            WHERE family.identity_scope_id = t.identity_scope_id
                              AND family.family_id = t.family_id
                              AND family.revoked_at IS NOT NULL
                        ) AS family_revoked
                    FROM identity_access.oidc_refresh_tokens AS t
                    INNER JOIN identity_access.user_sessions AS s
                      ON s.identity_scope_id = t.identity_scope_id
                     AND s.session_id = t.session_id
                     AND s.user_id = t.user_id
                    INNER JOIN identity_access.users AS u
                      ON u.identity_scope_id = t.identity_scope_id
                     AND u.user_id = t.user_id
                    WHERE t.identity_scope_id = @scope_id
                      AND t.family_id = @family_id
                      AND t.token_hash = @token_hash
                      AND t.client_id = @client_id
                      AND t.application_key = @application_key
                    LIMIT 1;
                    """,
                    connection,
                    transaction))
            {
                inspect.Parameters.AddWithValue(
                    "scope_id",
                    route.Request.IdentityScopeId);
                inspect.Parameters.AddWithValue(
                    "family_id",
                    familyId.Value);
                inspect.Parameters.AddWithValue(
                    "token_hash",
                    presentedTokenHash);
                inspect.Parameters.AddWithValue(
                    "client_id",
                    clientId);
                inspect.Parameters.AddWithValue(
                    "application_key",
                    route.Request.Application.Value);
                inspect.Parameters.AddWithValue(
                    "now",
                    now);
                inspect.Parameters.AddWithValue(
                    "active_user_status",
                    (short)UserStatus.Active);

                await using var reader =
                    await inspect
                        .ExecuteReaderAsync(
                            cancellationToken)
                        .ConfigureAwait(false);

                if (!await reader
                    .ReadAsync(
                        cancellationToken)
                    .ConfigureAwait(false))
                {
                    return OidcRefreshTokenRotationResult.Invalid();
                }

                tokenId = reader.GetGuid(0);
                sequenceNumber = reader.GetInt64(1);
                userId = reader.GetGuid(2);
                sessionId = reader.GetGuid(3);
                storedClientId = reader.GetString(4);
                application = new ApplicationKey(reader.GetString(5));
                authenticationContextKey = reader.GetString(6);
                scope = reader.GetString(7);
                authenticatedAt = reader.GetFieldValue<DateTimeOffset>(8);
                expiresAt = reader.GetFieldValue<DateTimeOffset>(9);
                consumedAt = reader.IsDBNull(10)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(10);
                revokedAt = reader.IsDBNull(11)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(11);
                sessionEligible = reader.GetBoolean(12);
                familyRevoked = reader.GetBoolean(13);
            }

            if (revokedAt is not null || familyRevoked)
            {
                return OidcRefreshTokenRotationResult.Invalid();
            }

            if (consumedAt is not null)
            {
                await using var revoke =
                    new NpgsqlCommand(
                        """
                        UPDATE identity_access.oidc_refresh_tokens
                        SET revoked_at = @now,
                            revocation_reason = 'reuse_detected'
                        WHERE identity_scope_id = @scope_id
                          AND family_id = @family_id
                          AND revoked_at IS NULL;
                        """,
                        connection,
                        transaction);

                revoke.Parameters.AddWithValue(
                    "scope_id",
                    route.Request.IdentityScopeId);
                revoke.Parameters.AddWithValue(
                    "family_id",
                    familyId.Value);
                revoke.Parameters.AddWithValue(
                    "now",
                    now);

                await revoke
                    .ExecuteNonQueryAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

                await transaction
                    .CommitAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

                return OidcRefreshTokenRotationResult.ReuseDetected();
            }

            if (!sessionEligible ||
                expiresAt <= now)
            {
                return OidcRefreshTokenRotationResult.Invalid();
            }

            await using (var consume =
                new NpgsqlCommand(
                    """
                    UPDATE identity_access.oidc_refresh_tokens
                    SET consumed_at = @now
                    WHERE identity_scope_id = @scope_id
                      AND token_id = @token_id
                      AND consumed_at IS NULL
                      AND revoked_at IS NULL;
                    """,
                    connection,
                    transaction))
            {
                consume.Parameters.AddWithValue(
                    "scope_id",
                    route.Request.IdentityScopeId);
                consume.Parameters.AddWithValue(
                    "token_id",
                    tokenId);
                consume.Parameters.AddWithValue(
                    "now",
                    now);

                if (await consume
                    .ExecuteNonQueryAsync(
                        cancellationToken)
                    .ConfigureAwait(false) != 1)
                {
                    throw new InvalidOperationException(
                        "Refresh-token state changed while the family mutation lock was held.");
                }
            }

            var replacement =
                new OidcRefreshTokenGrant(
                    familyId.Value,
                    replacementTokenId,
                    tokenId,
                    checked(sequenceNumber + 1),
                    new SubjectReference(
                        route.Request.IdentityScopeId,
                        userId),
                    sessionId,
                    storedClientId,
                    application,
                    authenticationContextKey,
                    scope,
                    authenticatedAt,
                    now,
                    expiresAt);

            await using (var insert =
                new NpgsqlCommand(
                    """
                    INSERT INTO identity_access.oidc_refresh_tokens
                    (
                        identity_scope_id,
                        family_id,
                        token_id,
                        parent_token_id,
                        sequence_number,
                        token_hash,
                        user_id,
                        session_id,
                        client_id,
                        application_key,
                        authentication_context_key,
                        scope,
                        authenticated_at,
                        issued_at,
                        expires_at
                    )
                    VALUES
                    (
                        @scope_id,
                        @family_id,
                        @token_id,
                        @parent_token_id,
                        @sequence_number,
                        @token_hash,
                        @user_id,
                        @session_id,
                        @client_id,
                        @application_key,
                        @authentication_context_key,
                        @scope,
                        @authenticated_at,
                        @issued_at,
                        @expires_at
                    );
                    """,
                    connection,
                    transaction))
            {
                AddGrantParameters(
                    insert,
                    replacement,
                    replacementTokenHash);

                await insert
                    .ExecuteNonQueryAsync(
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await transaction
                .CommitAsync(
                    cancellationToken)
                .ConfigureAwait(false);

            return OidcRefreshTokenRotationResult.Rotated(
                replacement);
        }

        private static void AddGrantParameters(
            NpgsqlCommand command,
            OidcRefreshTokenGrant grant,
            byte[] tokenHash)
        {
            command.Parameters.AddWithValue(
                "scope_id",
                grant.Subject.IdentityScopeId);
            command.Parameters.AddWithValue(
                "family_id",
                grant.FamilyId);
            command.Parameters.AddWithValue(
                "token_id",
                grant.TokenId);
            var parentTokenParameter =
                command.Parameters.Add(
                    "parent_token_id",
                    NpgsqlDbType.Uuid);
            parentTokenParameter.Value =
                (object?)grant.ParentTokenId ?? DBNull.Value;
            command.Parameters.AddWithValue(
                "sequence_number",
                grant.SequenceNumber);
            command.Parameters.AddWithValue(
                "token_hash",
                tokenHash);
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
                "scope",
                grant.Scope);
            command.Parameters.AddWithValue(
                "authenticated_at",
                grant.AuthenticatedAt);
            command.Parameters.AddWithValue(
                "issued_at",
                grant.IssuedAt);
            command.Parameters.AddWithValue(
                "expires_at",
                grant.ExpiresAt);
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
                    "OIDC refresh-token application does not match the resolved route.");
            }
        }
    }
}
