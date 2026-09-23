using System.Data.Common;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>PostgreSQL-backed recovery-code storage with atomic set replacement and single-use consumption.</summary>
    internal sealed class PostgreSqlRecoveryCodeStore : IRecoveryCodeStore
    {
        private readonly IIdentityDatabaseConnectionFactory _connectionFactory;

        public PostgreSqlRecoveryCodeStore(IIdentityDatabaseConnectionFactory connectionFactory)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> ReplaceActiveSetAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            IReadOnlyList<byte[]> codeHashes,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(authenticator);
            ArgumentNullException.ThrowIfNull(codeHashes);
            if (authenticator.Status != UserAuthenticatorStatus.Active || authenticator.ConfirmedAt is null)
                throw new ArgumentException("Recovery-code sets must be created as active authenticators.", nameof(authenticator));
            if (codeHashes.Count is < RecoveryCodeProviderOptions.MinimumCodeCount or > RecoveryCodeProviderOptions.MaximumCodeCount)
                throw new ArgumentOutOfRangeException(nameof(codeHashes));
            if (codeHashes.Any(hash => hash is null || hash.Length != 32))
                throw new ArgumentException("Recovery-code hashes must be SHA-256 values.", nameof(codeHashes));
            if (codeHashes.Select(Convert.ToHexString).Distinct(StringComparer.Ordinal).Count() != codeHashes.Count)
                throw new ArgumentException("Recovery-code hashes must be unique.", nameof(codeHashes));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (var userLock = connection.CreateCommand())
            {
                userLock.Transaction = transaction;
                userLock.CommandText = """
                    SELECT 1
                    FROM identity_access.users
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                    FOR UPDATE;
                    """;
                AddParameter(userLock, "scope", authenticator.IdentityScopeId);
                AddParameter(userLock, "user_id", authenticator.UserId);

                var exists = await userLock.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (exists is null or DBNull)
                    throw new InvalidOperationException("Recovery-code subject was not found in the resolved identity directory.");
            }

            int revokedCount;
            await using (var revokeCommand = connection.CreateCommand())
            {
                revokeCommand.Transaction = transaction;
                revokeCommand.CommandText = """
                    UPDATE identity_access.user_authenticators
                    SET status = @revoked_status,
                        revoked_at = @occurred_at,
                        row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND provider_key = @provider
                      AND status = @active_status;
                    """;
                AddParameter(revokeCommand, "revoked_status", (short)UserAuthenticatorStatus.Revoked);
                AddParameter(revokeCommand, "occurred_at", authenticator.CreatedAt);
                AddParameter(revokeCommand, "scope", authenticator.IdentityScopeId);
                AddParameter(revokeCommand, "user_id", authenticator.UserId);
                AddParameter(revokeCommand, "provider", RecoveryAuthenticationFactorProviderKey.Value);
                AddParameter(revokeCommand, "active_status", (short)UserAuthenticatorStatus.Active);
                revokedCount = await revokeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var genericCommand = connection.CreateCommand())
            {
                genericCommand.Transaction = transaction;
                genericCommand.CommandText = """
                    INSERT INTO identity_access.user_authenticators
                        (identity_scope_id, authenticator_id, user_id, provider_key, display_name,
                         status, created_at, confirmed_at, last_used_at, revoked_at)
                    VALUES
                        (@scope, @authenticator_id, @user_id, @provider, @display_name,
                         @status, @created_at, @confirmed_at, NULL, NULL);
                    """;
                AddParameter(genericCommand, "scope", authenticator.IdentityScopeId);
                AddParameter(genericCommand, "authenticator_id", authenticator.AuthenticatorId);
                AddParameter(genericCommand, "user_id", authenticator.UserId);
                AddParameter(genericCommand, "provider", RecoveryAuthenticationFactorProviderKey.Value);
                AddParameter(genericCommand, "display_name", authenticator.DisplayName);
                AddParameter(genericCommand, "status", (short)UserAuthenticatorStatus.Active);
                AddParameter(genericCommand, "created_at", authenticator.CreatedAt);
                AddParameter(genericCommand, "confirmed_at", authenticator.ConfirmedAt.Value);
                await genericCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var setCommand = connection.CreateCommand())
            {
                setCommand.Transaction = transaction;
                setCommand.CommandText = """
                    INSERT INTO identity_access.recovery_code_sets
                        (identity_scope_id, authenticator_id, hash_algorithm, code_count, created_at)
                    VALUES
                        (@scope, @authenticator_id, @hash_algorithm, @code_count, @created_at);
                    """;
                AddParameter(setCommand, "scope", authenticator.IdentityScopeId);
                AddParameter(setCommand, "authenticator_id", authenticator.AuthenticatorId);
                AddParameter(setCommand, "hash_algorithm", RecoveryCodeProviderOptions.HashAlgorithmName);
                AddParameter(setCommand, "code_count", (short)codeHashes.Count);
                AddParameter(setCommand, "created_at", authenticator.CreatedAt);
                await setCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var codeHash in codeHashes)
            {
                await using var codeCommand = connection.CreateCommand();
                codeCommand.Transaction = transaction;
                codeCommand.CommandText = """
                    INSERT INTO identity_access.recovery_codes
                        (identity_scope_id, authenticator_id, code_id, code_hash, created_at)
                    VALUES
                        (@scope, @authenticator_id, @code_id, @code_hash, @created_at);
                    """;
                AddParameter(codeCommand, "scope", authenticator.IdentityScopeId);
                AddParameter(codeCommand, "authenticator_id", authenticator.AuthenticatorId);
                AddParameter(codeCommand, "code_id", Guid.NewGuid());
                AddParameter(codeCommand, "code_hash", codeHash);
                AddParameter(codeCommand, "created_at", authenticator.CreatedAt);
                await codeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return revokedCount > 0;
        }

        public async Task<RecoveryCodeStoreMutationResult> TryConsumeAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            byte[] codeHash,
            DateTimeOffset consumedAt,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ValidateIdentifiers(identityScopeId, userId, authenticatorId);
            ArgumentNullException.ThrowIfNull(codeHash);
            if (codeHash.Length != 32) throw new ArgumentException("Recovery-code hash must be SHA-256.", nameof(codeHash));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            short status;
            await using (var lockSetCommand = connection.CreateCommand())
            {
                lockSetCommand.Transaction = transaction;
                lockSetCommand.CommandText = """
                    SELECT u.status
                    FROM identity_access.user_authenticators AS u
                    INNER JOIN identity_access.recovery_code_sets AS s
                        ON s.identity_scope_id = u.identity_scope_id
                       AND s.authenticator_id = u.authenticator_id
                    WHERE u.identity_scope_id = @scope
                      AND u.user_id = @user_id
                      AND u.authenticator_id = @authenticator_id
                      AND u.provider_key = @provider
                    FOR UPDATE OF u, s;
                    """;
                AddParameter(lockSetCommand, "scope", identityScopeId);
                AddParameter(lockSetCommand, "user_id", userId);
                AddParameter(lockSetCommand, "authenticator_id", authenticatorId);
                AddParameter(lockSetCommand, "provider", RecoveryAuthenticationFactorProviderKey.Value);

                var value = await lockSetCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (value is null or DBNull)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return RecoveryCodeStoreMutationResult.NotFound;
                }

                status = Convert.ToInt16(value);
            }

            if (status != (short)UserAuthenticatorStatus.Active)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return RecoveryCodeStoreMutationResult.NotActive;
            }

            DateTimeOffset? existingConsumedAt;
            await using (var lockCodeCommand = connection.CreateCommand())
            {
                lockCodeCommand.Transaction = transaction;
                lockCodeCommand.CommandText = """
                    SELECT consumed_at
                    FROM identity_access.recovery_codes
                    WHERE identity_scope_id = @scope
                      AND authenticator_id = @authenticator_id
                      AND code_hash = @code_hash
                    FOR UPDATE;
                    """;
                AddParameter(lockCodeCommand, "scope", identityScopeId);
                AddParameter(lockCodeCommand, "authenticator_id", authenticatorId);
                AddParameter(lockCodeCommand, "code_hash", codeHash);

                await using var reader = await lockCodeCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return RecoveryCodeStoreMutationResult.InvalidCode;
                }

                existingConsumedAt = reader.IsDBNull(0)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(0);
            }

            if (existingConsumedAt is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return RecoveryCodeStoreMutationResult.AlreadyConsumed;
            }

            await using (var consumeCommand = connection.CreateCommand())
            {
                consumeCommand.Transaction = transaction;
                consumeCommand.CommandText = """
                    UPDATE identity_access.recovery_codes
                    SET consumed_at = @consumed_at
                    WHERE identity_scope_id = @scope
                      AND authenticator_id = @authenticator_id
                      AND code_hash = @code_hash
                      AND consumed_at IS NULL;
                    """;
                AddParameter(consumeCommand, "consumed_at", consumedAt);
                AddParameter(consumeCommand, "scope", identityScopeId);
                AddParameter(consumeCommand, "authenticator_id", authenticatorId);
                AddParameter(consumeCommand, "code_hash", codeHash);
                if (await consumeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                    throw new InvalidOperationException("Recovery-code consumption lost its locked row.");
            }

            await using (var setCommand = connection.CreateCommand())
            {
                setCommand.Transaction = transaction;
                setCommand.CommandText = """
                    UPDATE identity_access.recovery_code_sets
                    SET row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND authenticator_id = @authenticator_id;
                    """;
                AddParameter(setCommand, "scope", identityScopeId);
                AddParameter(setCommand, "authenticator_id", authenticatorId);
                await setCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var genericCommand = connection.CreateCommand())
            {
                genericCommand.Transaction = transaction;
                genericCommand.CommandText = """
                    UPDATE identity_access.user_authenticators
                    SET last_used_at = @consumed_at,
                        row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND user_id = @user_id
                      AND authenticator_id = @authenticator_id
                      AND provider_key = @provider;
                    """;
                AddParameter(genericCommand, "consumed_at", consumedAt);
                AddParameter(genericCommand, "scope", identityScopeId);
                AddParameter(genericCommand, "user_id", userId);
                AddParameter(genericCommand, "authenticator_id", authenticatorId);
                AddParameter(genericCommand, "provider", RecoveryAuthenticationFactorProviderKey.Value);
                await genericCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return RecoveryCodeStoreMutationResult.Succeeded;
        }

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
