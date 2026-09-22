using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Authentication
{

    /// <summary>Provides persistence operations for PostgreSQL password credential.</summary>
    internal sealed class PostgreSqlPasswordCredentialStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IPasswordCredentialStore
    {
        /// <summary>Finds a password credential by normalized login identifier.</summary>
        public async Task<VersionedRecord<PasswordCredential>?> FindByLoginAsync(ResolvedDatabaseRoute route,
            string normalizedLoginIdentifier, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(normalizedLoginIdentifier);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT user_id, login_identifier, password_hash, failed_access_count, lockout_until, row_version
                FROM identity_access.password_credentials
                WHERE identity_scope_id = @scope AND normalized_login_identifier = @login;
                """, connection);
            command.Parameters.AddWithValue("scope", route.Request.IdentityScopeId);
            command.Parameters.AddWithValue("login", normalizedLoginIdentifier);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            return Read(route.Request.IdentityScopeId, reader);
        }

        /// <summary>Gets a password credential by subject.</summary>
        public async Task<VersionedRecord<PasswordCredential>?> GetBySubjectAsync(ResolvedDatabaseRoute route,
            SubjectReference subject, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(subject);
            PostgreSqlDirectoryGuard.EnsureScope(route, subject.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT user_id, login_identifier, password_hash, failed_access_count, lockout_until, row_version
                FROM identity_access.password_credentials
                WHERE identity_scope_id = @scope AND user_id = @user_id;
                """, connection);
            command.Parameters.AddWithValue("scope", subject.IdentityScopeId);
            command.Parameters.AddWithValue("user_id", subject.UserId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            return Read(subject.IdentityScopeId, reader);
        }

        /// <summary>Creates a password credential record in the resolved database route.</summary>
        public async Task<VersionedRecord<PasswordCredential>> CreateAsync(ResolvedDatabaseRoute route,
            PasswordCredential credential, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(credential);
            PostgreSqlDirectoryGuard.EnsureScope(route, credential.Subject.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.password_credentials
                    (identity_scope_id, user_id, login_identifier, normalized_login_identifier, password_hash)
                VALUES (@scope, @user_id, @login, @normalized_login, @password_hash)
                RETURNING row_version;
                """, connection);
            command.Parameters.AddWithValue("scope", credential.Subject.IdentityScopeId);
            command.Parameters.AddWithValue("user_id", credential.Subject.UserId);
            command.Parameters.AddWithValue("login", credential.LoginIdentifier.Value);
            command.Parameters.AddWithValue("normalized_login", credential.LoginIdentifier.NormalizedValue);
            command.Parameters.AddWithValue("password_hash", credential.PasswordHash);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted credential did not return a row version."));
            return new VersionedRecord<PasswordCredential>(credential, version);
        }

        /// <summary>Updates the password hash using optimistic concurrency.</summary>
        public async Task<VersionedRecord<PasswordCredential>> UpdatePasswordAsync(ResolvedDatabaseRoute route,
            PasswordCredential credential, long expectedVersion, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(credential);
            PostgreSqlDirectoryGuard.EnsureScope(route, credential.Subject.IdentityScopeId);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.password_credentials
                SET login_identifier = @login,
                    normalized_login_identifier = @normalized_login,
                    password_hash = @password_hash,
                    failed_access_count = 0,
                    lockout_until = NULL,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope AND user_id = @user_id AND row_version = @expected_version
                RETURNING row_version;
                """, connection);
            command.Parameters.AddWithValue("scope", credential.Subject.IdentityScopeId);
            command.Parameters.AddWithValue("user_id", credential.Subject.UserId);
            command.Parameters.AddWithValue("login", credential.LoginIdentifier.Value);
            command.Parameters.AddWithValue("normalized_login", credential.LoginIdentifier.NormalizedValue);
            command.Parameters.AddWithValue("password_hash", credential.PasswordHash);
            command.Parameters.AddWithValue("expected_version", expectedVersion);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            var updated = new PasswordCredential(credential.Subject, credential.LoginIdentifier, credential.PasswordHash);
            return new VersionedRecord<PasswordCredential>(updated, (long)result);
        }

        /// <summary>Records a failed password credential attempt and returns the updated persisted state.</summary>
        public async Task<VersionedRecord<PasswordCredential>> RecordFailureAsync(ResolvedDatabaseRoute route,
            SubjectReference subject, int lockoutThreshold, DateTimeOffset lockoutUntil,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(subject);
            PostgreSqlDirectoryGuard.EnsureScope(route, subject.IdentityScopeId);
            if (lockoutThreshold < 2) throw new ArgumentOutOfRangeException(nameof(lockoutThreshold));
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.password_credentials
                SET failed_access_count = failed_access_count + 1,
                    lockout_until = CASE
                        WHEN failed_access_count + 1 >= @threshold THEN @lockout_until
                        ELSE lockout_until
                    END,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope AND user_id = @user_id
                RETURNING login_identifier, password_hash, failed_access_count, lockout_until, row_version;
                """, connection);
            command.Parameters.AddWithValue("scope", subject.IdentityScopeId);
            command.Parameters.AddWithValue("user_id", subject.UserId);
            command.Parameters.AddWithValue("threshold", lockoutThreshold);
            command.Parameters.AddWithValue("lockout_until", lockoutUntil);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The credential no longer exists.");
            var value = new PasswordCredential(subject, new LoginIdentifier(reader.GetString(0)), reader.GetString(1),
                reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
            return new VersionedRecord<PasswordCredential>(value, reader.GetInt64(4));
        }

        /// <summary>Records a successful password credential operation and clears failure state as required.</summary>
        public async Task<VersionedRecord<PasswordCredential>> RecordSuccessAsync(ResolvedDatabaseRoute route,
            SubjectReference subject, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(subject);
            PostgreSqlDirectoryGuard.EnsureScope(route, subject.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.password_credentials
                SET failed_access_count = 0,
                    lockout_until = NULL,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope AND user_id = @user_id
                RETURNING login_identifier, password_hash, failed_access_count, lockout_until, row_version;
                """, connection);
            command.Parameters.AddWithValue("scope", subject.IdentityScopeId);
            command.Parameters.AddWithValue("user_id", subject.UserId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The credential no longer exists.");
            var value = new PasswordCredential(subject, new LoginIdentifier(reader.GetString(0)), reader.GetString(1),
                reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
            return new VersionedRecord<PasswordCredential>(value, reader.GetInt64(4));
        }

        private static VersionedRecord<PasswordCredential> Read(Guid scope, NpgsqlDataReader reader)
        {
            var subject = new SubjectReference(scope, reader.GetGuid(0));
            var value = new PasswordCredential(subject, new LoginIdentifier(reader.GetString(1)), reader.GetString(2),
                reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4));
            return new VersionedRecord<PasswordCredential>(value, reader.GetInt64(5));
        }
    }
}
