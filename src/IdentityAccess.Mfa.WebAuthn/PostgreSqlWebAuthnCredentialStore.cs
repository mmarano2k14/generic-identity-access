using System.Data.Common;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>PostgreSQL-backed WebAuthn registration and authentication state with atomic challenge consumption.</summary>
    internal sealed class PostgreSqlWebAuthnCredentialStore : IWebAuthnCredentialStore
    {
        private readonly IIdentityDatabaseConnectionFactory _connectionFactory;

        public PostgreSqlWebAuthnCredentialStore(IIdentityDatabaseConnectionFactory connectionFactory)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);
            _connectionFactory = connectionFactory;
        }

        public async Task CreatePendingRegistrationAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            ApplicationKey application,
            byte[] challengeHash,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(authenticator);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(challengeHash);
            if (challengeHash.Length != 32) throw new ArgumentException("WebAuthn challenge hash must contain 32 bytes.", nameof(challengeHash));
            if (authenticator.Status != UserAuthenticatorStatus.Pending) throw new ArgumentException("WebAuthn registration must begin in Pending status.", nameof(authenticator));
            if (expiresAt <= authenticator.CreatedAt) throw new ArgumentOutOfRangeException(nameof(expiresAt));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (var genericCommand = connection.CreateCommand())
            {
                genericCommand.Transaction = transaction;
                genericCommand.CommandText = """
                    INSERT INTO identity_access.user_authenticators
                        (identity_scope_id, authenticator_id, user_id, provider_key, display_name,
                         status, created_at, confirmed_at, last_used_at, revoked_at)
                    VALUES
                        (@scope, @authenticator_id, @user_id, @provider, @display_name,
                         @status, @created_at, NULL, NULL, NULL);
                    """;
                AddParameter(genericCommand, "scope", authenticator.IdentityScopeId);
                AddParameter(genericCommand, "authenticator_id", authenticator.AuthenticatorId);
                AddParameter(genericCommand, "user_id", authenticator.UserId);
                AddParameter(genericCommand, "provider", WebAuthnAuthenticationFactorProviderKey.Value);
                AddParameter(genericCommand, "display_name", authenticator.DisplayName);
                AddParameter(genericCommand, "status", (short)UserAuthenticatorStatus.Pending);
                AddParameter(genericCommand, "created_at", authenticator.CreatedAt);
                await genericCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var challengeCommand = connection.CreateCommand())
            {
                challengeCommand.Transaction = transaction;
                challengeCommand.CommandText = """
                    INSERT INTO identity_access.webauthn_registration_challenges
                        (identity_scope_id, authenticator_id, application_key, challenge_hash,
                         expires_at, consumed_at, created_at, updated_at)
                    VALUES
                        (@scope, @authenticator_id, @application, @challenge_hash,
                         @expires_at, NULL, @created_at, @created_at);
                    """;
                AddParameter(challengeCommand, "scope", authenticator.IdentityScopeId);
                AddParameter(challengeCommand, "authenticator_id", authenticator.AuthenticatorId);
                AddParameter(challengeCommand, "application", application.Value);
                AddParameter(challengeCommand, "challenge_hash", challengeHash);
                AddParameter(challengeCommand, "expires_at", expiresAt);
                AddParameter(challengeCommand, "created_at", authenticator.CreatedAt);
                await challengeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<WebAuthnPendingRegistrationState?> GetPendingRegistrationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ValidateIdentifiers(identityScopeId, userId, authenticatorId);

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT u.status,
                       c.application_key,
                       c.challenge_hash,
                       c.expires_at,
                       c.consumed_at
                FROM identity_access.user_authenticators AS u
                INNER JOIN identity_access.webauthn_registration_challenges AS c
                    ON c.identity_scope_id = u.identity_scope_id
                   AND c.authenticator_id = u.authenticator_id
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.authenticator_id = @authenticator_id
                  AND u.provider_key = @provider;
                """;
            AddParameter(command, "scope", identityScopeId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "authenticator_id", authenticatorId);
            AddParameter(command, "provider", WebAuthnAuthenticationFactorProviderKey.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            return new WebAuthnPendingRegistrationState(
                (UserAuthenticatorStatus)reader.GetInt16(0),
                new ApplicationKey(reader.GetString(1)),
                reader.GetFieldValue<byte[]>(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4));
        }

        public async Task<IReadOnlyList<byte[]>> ListActiveCredentialIdsAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT w.credential_id
                FROM identity_access.webauthn_credentials AS w
                INNER JOIN identity_access.user_authenticators AS u
                    ON u.identity_scope_id = w.identity_scope_id
                   AND u.authenticator_id = w.authenticator_id
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.provider_key = @provider
                  AND u.status = @active_status
                ORDER BY u.authenticator_id;
                """;
            AddParameter(command, "scope", identityScopeId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "provider", WebAuthnAuthenticationFactorProviderKey.Value);
            AddParameter(command, "active_status", (short)UserAuthenticatorStatus.Active);

            var result = new List<byte[]>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                result.Add(reader.GetFieldValue<byte[]>(0));
            return result;
        }

        public async Task<WebAuthnRegistrationStoreResult> TryCompleteRegistrationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            ApplicationKey application,
            WebAuthnCredentialMaterial credential,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(credential);
            ValidateIdentifiers(identityScopeId, userId, authenticatorId);

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            short status;
            string persistedApplication;
            DateTimeOffset expiresAt;
            DateTimeOffset? consumedAt;

            await using (var lockCommand = connection.CreateCommand())
            {
                lockCommand.Transaction = transaction;
                lockCommand.CommandText = """
                    SELECT u.status,
                           c.application_key,
                           c.expires_at,
                           c.consumed_at
                    FROM identity_access.user_authenticators AS u
                    INNER JOIN identity_access.webauthn_registration_challenges AS c
                        ON c.identity_scope_id = u.identity_scope_id
                       AND c.authenticator_id = u.authenticator_id
                    WHERE u.identity_scope_id = @scope
                      AND u.user_id = @user_id
                      AND u.authenticator_id = @authenticator_id
                      AND u.provider_key = @provider
                    FOR UPDATE OF u, c;
                    """;
                AddParameter(lockCommand, "scope", identityScopeId);
                AddParameter(lockCommand, "user_id", userId);
                AddParameter(lockCommand, "authenticator_id", authenticatorId);
                AddParameter(lockCommand, "provider", WebAuthnAuthenticationFactorProviderKey.Value);

                await using var reader = await lockCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return WebAuthnRegistrationStoreResult.NotFound;
                }

                status = reader.GetInt16(0);
                persistedApplication = reader.GetString(1);
                expiresAt = reader.GetFieldValue<DateTimeOffset>(2);
                consumedAt = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3);
            }

            if (status != (short)UserAuthenticatorStatus.Pending ||
                !string.Equals(persistedApplication, application.Value, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnRegistrationStoreResult.InvalidState;
            }

            if (consumedAt is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnRegistrationStoreResult.AlreadyUsed;
            }

            if (completedAt > expiresAt)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnRegistrationStoreResult.Expired;
            }

            int inserted;
            await using (var credentialCommand = connection.CreateCommand())
            {
                credentialCommand.Transaction = transaction;
                credentialCommand.CommandText = """
                    INSERT INTO identity_access.webauthn_credentials
                        (identity_scope_id, authenticator_id, credential_id, cose_public_key,
                         cose_algorithm, aaguid, sign_count, backup_eligible, backup_state,
                         user_handle, created_at, updated_at)
                    VALUES
                        (@scope, @authenticator_id, @credential_id, @cose_public_key,
                         @cose_algorithm, @aaguid, @sign_count, @backup_eligible, @backup_state,
                         @user_handle, @completed_at, @completed_at)
                    ON CONFLICT (identity_scope_id, credential_id) DO NOTHING;
                    """;
                AddParameter(credentialCommand, "scope", identityScopeId);
                AddParameter(credentialCommand, "authenticator_id", authenticatorId);
                AddParameter(credentialCommand, "credential_id", credential.CredentialId);
                AddParameter(credentialCommand, "cose_public_key", credential.CosePublicKey);
                AddParameter(credentialCommand, "cose_algorithm", (short)credential.CoseAlgorithm);
                AddParameter(credentialCommand, "aaguid", credential.Aaguid);
                AddParameter(credentialCommand, "sign_count", credential.SignCount);
                AddParameter(credentialCommand, "backup_eligible", credential.BackupEligible);
                AddParameter(credentialCommand, "backup_state", credential.BackupState);
                AddParameter(credentialCommand, "user_handle", credential.UserHandle);
                AddParameter(credentialCommand, "completed_at", completedAt);
                inserted = await credentialCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            if (inserted != 1)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnRegistrationStoreResult.CredentialAlreadyRegistered;
            }

            await using (var genericCommand = connection.CreateCommand())
            {
                genericCommand.Transaction = transaction;
                genericCommand.CommandText = """
                    UPDATE identity_access.user_authenticators
                    SET status = @active_status,
                        confirmed_at = @completed_at,
                        last_used_at = NULL,
                        row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND authenticator_id = @authenticator_id
                      AND provider_key = @provider;
                    """;
                AddParameter(genericCommand, "active_status", (short)UserAuthenticatorStatus.Active);
                AddParameter(genericCommand, "completed_at", completedAt);
                AddParameter(genericCommand, "scope", identityScopeId);
                AddParameter(genericCommand, "user_id", userId);
                AddParameter(genericCommand, "authenticator_id", authenticatorId);
                AddParameter(genericCommand, "provider", WebAuthnAuthenticationFactorProviderKey.Value);
                await genericCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var challengeCommand = connection.CreateCommand())
            {
                challengeCommand.Transaction = transaction;
                challengeCommand.CommandText = """
                    UPDATE identity_access.webauthn_registration_challenges
                    SET consumed_at = @completed_at,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND authenticator_id = @authenticator_id;
                    """;
                AddParameter(challengeCommand, "completed_at", completedAt);
                AddParameter(challengeCommand, "scope", identityScopeId);
                AddParameter(challengeCommand, "authenticator_id", authenticatorId);
                await challengeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return WebAuthnRegistrationStoreResult.Succeeded;
        }

        public async Task CreateAuthenticationChallengeAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid challengeId,
            ApplicationKey application,
            byte[] challengeHash,
            DateTimeOffset createdAt,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(application);
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            if (challengeId == Guid.Empty) throw new ArgumentException("Challenge identifier is required.", nameof(challengeId));
            ArgumentNullException.ThrowIfNull(challengeHash);
            if (challengeHash.Length != 32) throw new ArgumentException("WebAuthn challenge hash must contain 32 bytes.", nameof(challengeHash));
            if (expiresAt <= createdAt) throw new ArgumentOutOfRangeException(nameof(expiresAt));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO identity_access.webauthn_authentication_challenges
                    (identity_scope_id, challenge_id, user_id, application_key, challenge_hash,
                     expires_at, consumed_at, created_at, updated_at)
                VALUES
                    (@scope, @challenge_id, @user_id, @application, @challenge_hash,
                     @expires_at, NULL, @created_at, @created_at);
                """;
            AddParameter(command, "scope", identityScopeId);
            AddParameter(command, "challenge_id", challengeId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "application", application.Value);
            AddParameter(command, "challenge_hash", challengeHash);
            AddParameter(command, "expires_at", expiresAt);
            AddParameter(command, "created_at", createdAt);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<WebAuthnAuthenticationChallengeState?> GetAuthenticationChallengeAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid challengeId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            if (challengeId == Guid.Empty) throw new ArgumentException("Challenge identifier is required.", nameof(challengeId));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT application_key,
                       challenge_hash,
                       expires_at,
                       consumed_at
                FROM identity_access.webauthn_authentication_challenges
                WHERE identity_scope_id = @scope
                  AND user_id = @user_id
                  AND challenge_id = @challenge_id;
                """;
            AddParameter(command, "scope", identityScopeId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "challenge_id", challengeId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            return new WebAuthnAuthenticationChallengeState(
                new ApplicationKey(reader.GetString(0)),
                reader.GetFieldValue<byte[]>(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
        }

        public async Task<IReadOnlyList<WebAuthnCredentialRecord>> ListActiveCredentialsAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT u.authenticator_id,
                       u.status,
                       w.credential_id,
                       w.cose_public_key,
                       w.cose_algorithm,
                       w.sign_count,
                       w.backup_eligible,
                       w.backup_state,
                       w.user_handle
                FROM identity_access.user_authenticators AS u
                INNER JOIN identity_access.webauthn_credentials AS w
                    ON w.identity_scope_id = u.identity_scope_id
                   AND w.authenticator_id = u.authenticator_id
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.provider_key = @provider
                  AND u.status = @active_status
                ORDER BY u.authenticator_id;
                """;
            AddParameter(command, "scope", identityScopeId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "provider", WebAuthnAuthenticationFactorProviderKey.Value);
            AddParameter(command, "active_status", (short)UserAuthenticatorStatus.Active);

            var result = new List<WebAuthnCredentialRecord>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                result.Add(ReadCredentialRecord(reader));
            return result;
        }

        public async Task<WebAuthnCredentialRecord?> GetActiveCredentialAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            byte[] credentialId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            ArgumentNullException.ThrowIfNull(credentialId);
            if (credentialId.Length is 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(credentialId));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT u.authenticator_id,
                       u.status,
                       w.credential_id,
                       w.cose_public_key,
                       w.cose_algorithm,
                       w.sign_count,
                       w.backup_eligible,
                       w.backup_state,
                       w.user_handle
                FROM identity_access.user_authenticators AS u
                INNER JOIN identity_access.webauthn_credentials AS w
                    ON w.identity_scope_id = u.identity_scope_id
                   AND w.authenticator_id = u.authenticator_id
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.provider_key = @provider
                  AND u.status = @active_status
                  AND w.credential_id = @credential_id;
                """;
            AddParameter(command, "scope", identityScopeId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "provider", WebAuthnAuthenticationFactorProviderKey.Value);
            AddParameter(command, "active_status", (short)UserAuthenticatorStatus.Active);
            AddParameter(command, "credential_id", credentialId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                ? ReadCredentialRecord(reader)
                : null;
        }

        public async Task<WebAuthnAuthenticationStoreResult> TryCompleteAuthenticationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid challengeId,
            ApplicationKey application,
            byte[] credentialId,
            long assertedSignCount,
            bool assertedBackupEligible,
            bool assertedBackupState,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(application);
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            if (challengeId == Guid.Empty) throw new ArgumentException("Challenge identifier is required.", nameof(challengeId));
            ArgumentNullException.ThrowIfNull(credentialId);
            if (credentialId.Length is 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(credentialId));
            if (assertedSignCount is < 0 or > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(assertedSignCount));
            if (assertedBackupState && !assertedBackupEligible) throw new ArgumentException("Backup state requires backup eligibility.", nameof(assertedBackupState));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            string persistedApplication;
            DateTimeOffset expiresAt;
            DateTimeOffset? consumedAt;
            short status;
            long persistedSignCount;
            bool persistedBackupEligible;

            await using (var lockCommand = connection.CreateCommand())
            {
                lockCommand.Transaction = transaction;
                lockCommand.CommandText = """
                    SELECT c.application_key,
                           c.expires_at,
                           c.consumed_at,
                           u.status,
                           w.sign_count,
                           w.backup_eligible
                    FROM identity_access.webauthn_authentication_challenges AS c
                    INNER JOIN identity_access.user_authenticators AS u
                        ON u.identity_scope_id = c.identity_scope_id
                       AND u.user_id = c.user_id
                    INNER JOIN identity_access.webauthn_credentials AS w
                        ON w.identity_scope_id = u.identity_scope_id
                       AND w.authenticator_id = u.authenticator_id
                    WHERE c.identity_scope_id = @scope
                      AND c.user_id = @user_id
                      AND c.challenge_id = @challenge_id
                      AND u.provider_key = @provider
                      AND w.credential_id = @credential_id
                    FOR UPDATE OF c, u, w;
                    """;
                AddParameter(lockCommand, "scope", identityScopeId);
                AddParameter(lockCommand, "user_id", userId);
                AddParameter(lockCommand, "challenge_id", challengeId);
                AddParameter(lockCommand, "provider", WebAuthnAuthenticationFactorProviderKey.Value);
                AddParameter(lockCommand, "credential_id", credentialId);

                await using var reader = await lockCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return WebAuthnAuthenticationStoreResult.NotFound;
                }

                persistedApplication = reader.GetString(0);
                expiresAt = reader.GetFieldValue<DateTimeOffset>(1);
                consumedAt = reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2);
                status = reader.GetInt16(3);
                persistedSignCount = reader.GetInt64(4);
                persistedBackupEligible = reader.GetBoolean(5);
            }

            if (status != (short)UserAuthenticatorStatus.Active ||
                !string.Equals(persistedApplication, application.Value, StringComparison.Ordinal) ||
                persistedBackupEligible != assertedBackupEligible)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationStoreResult.InvalidState;
            }

            if (consumedAt is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationStoreResult.AlreadyUsed;
            }

            if (completedAt > expiresAt)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationStoreResult.Expired;
            }

            if ((persistedSignCount != 0 || assertedSignCount != 0) && assertedSignCount <= persistedSignCount)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return WebAuthnAuthenticationStoreResult.ReplayDetected;
            }

            await using (var authenticatorCommand = connection.CreateCommand())
            {
                authenticatorCommand.Transaction = transaction;
                authenticatorCommand.CommandText = """
                    UPDATE identity_access.user_authenticators
                    SET last_used_at = @completed_at,
                        row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND provider_key = @provider
                      AND authenticator_id =
                      (
                          SELECT authenticator_id
                          FROM identity_access.webauthn_credentials
                          WHERE identity_scope_id = @scope
                            AND credential_id = @credential_id
                      );
                    """;
                AddParameter(authenticatorCommand, "completed_at", completedAt);
                AddParameter(authenticatorCommand, "scope", identityScopeId);
                AddParameter(authenticatorCommand, "user_id", userId);
                AddParameter(authenticatorCommand, "provider", WebAuthnAuthenticationFactorProviderKey.Value);
                AddParameter(authenticatorCommand, "credential_id", credentialId);
                await authenticatorCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var credentialCommand = connection.CreateCommand())
            {
                credentialCommand.Transaction = transaction;
                credentialCommand.CommandText = """
                    UPDATE identity_access.webauthn_credentials
                    SET sign_count = @sign_count,
                        backup_state = @backup_state,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND credential_id = @credential_id;
                    """;
                AddParameter(credentialCommand, "sign_count", assertedSignCount);
                AddParameter(credentialCommand, "backup_state", assertedBackupState);
                AddParameter(credentialCommand, "scope", identityScopeId);
                AddParameter(credentialCommand, "credential_id", credentialId);
                await credentialCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var challengeCommand = connection.CreateCommand())
            {
                challengeCommand.Transaction = transaction;
                challengeCommand.CommandText = """
                    UPDATE identity_access.webauthn_authentication_challenges
                    SET consumed_at = @completed_at,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND challenge_id = @challenge_id;
                    """;
                AddParameter(challengeCommand, "completed_at", completedAt);
                AddParameter(challengeCommand, "scope", identityScopeId);
                AddParameter(challengeCommand, "user_id", userId);
                AddParameter(challengeCommand, "challenge_id", challengeId);
                await challengeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return WebAuthnAuthenticationStoreResult.Succeeded;
        }

        private static WebAuthnCredentialRecord ReadCredentialRecord(DbDataReader reader) =>
            new(
                reader.GetGuid(0),
                (UserAuthenticatorStatus)reader.GetInt16(1),
                reader.GetFieldValue<byte[]>(2),
                reader.GetFieldValue<byte[]>(3),
                reader.GetInt16(4),
                reader.GetInt64(5),
                reader.GetBoolean(6),
                reader.GetBoolean(7),
                reader.GetFieldValue<byte[]>(8));

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static void ValidateIdentifiers(Guid identityScopeId, Guid userId, Guid authenticatorId)
        {
            if (identityScopeId == Guid.Empty) throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
            if (authenticatorId == Guid.Empty) throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
        }
    }
}
