using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Authentication
{
    /// <summary>PostgreSQL persistence for generic user-authenticator metadata.</summary>
    internal sealed class PostgreSqlUserAuthenticatorStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IUserAuthenticatorStore
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<VersionedRecord<UserAuthenticator>>> ListByUserAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT authenticator_id, provider_key, display_name, status, row_version,
                       created_at, confirmed_at, last_used_at, revoked_at
                FROM identity_access.user_authenticators
                WHERE identity_scope_id = @scope
                  AND user_id = @user_id
                ORDER BY created_at, authenticator_id;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            command.Parameters.AddWithValue("user_id", userId);

            var records = new List<VersionedRecord<UserAuthenticator>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                records.Add(Read(reader, identityScopeId, userId));
            }

            return records;
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<UserAuthenticator>?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid authenticatorId,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            if (authenticatorId == Guid.Empty)
                throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT user_id, provider_key, display_name, status, row_version,
                       created_at, confirmed_at, last_used_at, revoked_at
                FROM identity_access.user_authenticators
                WHERE identity_scope_id = @scope
                  AND authenticator_id = @authenticator_id;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            command.Parameters.AddWithValue("authenticator_id", authenticatorId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            var userId = reader.GetGuid(0);
            return Read(reader, identityScopeId, userId, authenticatorId, columnOffset: 1);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<UserAuthenticator>> CreateAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(authenticator);
            PostgreSqlDirectoryGuard.EnsureScope(route, authenticator.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.user_authenticators
                    (identity_scope_id, authenticator_id, user_id, provider_key, display_name,
                     status, created_at, confirmed_at, last_used_at, revoked_at)
                VALUES
                    (@scope, @authenticator_id, @user_id, @provider, @display_name,
                     @status, @created_at, @confirmed_at, @last_used_at, @revoked_at)
                RETURNING row_version;
                """, connection);
            AddParameters(command, authenticator);

            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted authenticator did not return a row version."));
            return new VersionedRecord<UserAuthenticator>(authenticator, version);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<UserAuthenticator>> UpdateAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(authenticator);
            PostgreSqlDirectoryGuard.EnsureScope(route, authenticator.IdentityScopeId);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.user_authenticators
                SET display_name = @display_name,
                    status = @status,
                    confirmed_at = @confirmed_at,
                    last_used_at = @last_used_at,
                    revoked_at = @revoked_at,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND authenticator_id = @authenticator_id
                  AND user_id = @user_id
                  AND provider_key = @provider
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection);
            AddParameters(command, authenticator);
            command.Parameters.AddWithValue("expected_version", expectedVersion);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<UserAuthenticator>(authenticator, (long)result);
        }

        private static void AddParameters(NpgsqlCommand command, UserAuthenticator authenticator)
        {
            command.Parameters.AddWithValue("scope", authenticator.IdentityScopeId);
            command.Parameters.AddWithValue("authenticator_id", authenticator.AuthenticatorId);
            command.Parameters.AddWithValue("user_id", authenticator.UserId);
            command.Parameters.AddWithValue("provider", authenticator.Provider.Value);
            command.Parameters.AddWithValue("display_name", authenticator.DisplayName);
            command.Parameters.AddWithValue("status", (short)authenticator.Status);
            command.Parameters.AddWithValue("created_at", authenticator.CreatedAt);
            command.Parameters.AddWithValue("confirmed_at", (object?)authenticator.ConfirmedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("last_used_at", (object?)authenticator.LastUsedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("revoked_at", (object?)authenticator.RevokedAt ?? DBNull.Value);
        }

        private static VersionedRecord<UserAuthenticator> Read(
            NpgsqlDataReader reader,
            Guid identityScopeId,
            Guid userId)
        {
            var authenticatorId = reader.GetGuid(0);
            return Read(reader, identityScopeId, userId, authenticatorId, columnOffset: 1);
        }

        private static VersionedRecord<UserAuthenticator> Read(
            NpgsqlDataReader reader,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            int columnOffset)
        {
            var provider = new AuthenticationFactorProviderKey(reader.GetString(columnOffset));
            var displayName = reader.GetString(columnOffset + 1);
            var status = (UserAuthenticatorStatus)reader.GetInt16(columnOffset + 2);
            var version = reader.GetInt64(columnOffset + 3);
            var createdAt = reader.GetFieldValue<DateTimeOffset>(columnOffset + 4);
            DateTimeOffset? confirmedAt = reader.IsDBNull(columnOffset + 5)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(columnOffset + 5);
            DateTimeOffset? lastUsedAt = reader.IsDBNull(columnOffset + 6)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(columnOffset + 6);
            DateTimeOffset? revokedAt = reader.IsDBNull(columnOffset + 7)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(columnOffset + 7);

            var authenticator = new UserAuthenticator(
                identityScopeId,
                authenticatorId,
                userId,
                provider,
                displayName,
                status,
                createdAt,
                confirmedAt,
                lastUsedAt,
                revokedAt);

            return new VersionedRecord<UserAuthenticator>(authenticator, version);
        }
    }
}
