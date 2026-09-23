using System.Data.Common;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>PostgreSQL-backed TOTP storage with row locking around accepted time-step mutations.</summary>
    internal sealed class PostgreSqlTotpAuthenticatorStore : ITotpAuthenticatorStore
    {
        private readonly IIdentityDatabaseConnectionFactory _connectionFactory;

        public PostgreSqlTotpAuthenticatorStore(IIdentityDatabaseConnectionFactory connectionFactory)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);
            _connectionFactory = connectionFactory;
        }

        public async Task CreatePendingAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            byte[] protectedSecret,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(authenticator);
            ArgumentNullException.ThrowIfNull(protectedSecret);
            if (protectedSecret.Length == 0) throw new ArgumentException("Protected TOTP secret is required.", nameof(protectedSecret));
            if (authenticator.Status != UserAuthenticatorStatus.Pending)
                throw new ArgumentException("TOTP enrollment must begin in Pending status.", nameof(authenticator));

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
                AddParameter(genericCommand, "provider", TotpAuthenticationFactorProviderKey.Value);
                AddParameter(genericCommand, "display_name", authenticator.DisplayName);
                AddParameter(genericCommand, "status", (short)UserAuthenticatorStatus.Pending);
                AddParameter(genericCommand, "created_at", authenticator.CreatedAt);
                await genericCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var providerCommand = connection.CreateCommand())
            {
                providerCommand.Transaction = transaction;
                providerCommand.CommandText = """
                    INSERT INTO identity_access.totp_authenticators
                        (identity_scope_id, authenticator_id, protected_secret,
                         algorithm, digits, period_seconds, created_at)
                    VALUES
                        (@scope, @authenticator_id, @protected_secret,
                         @algorithm, @digits, @period_seconds, @created_at);
                    """;
                AddParameter(providerCommand, "scope", authenticator.IdentityScopeId);
                AddParameter(providerCommand, "authenticator_id", authenticator.AuthenticatorId);
                AddParameter(providerCommand, "protected_secret", protectedSecret);
                AddParameter(providerCommand, "algorithm", TotpProviderOptions.AlgorithmName);
                AddParameter(providerCommand, "digits", (short)TotpProviderOptions.Digits);
                AddParameter(providerCommand, "period_seconds", (short)TotpProviderOptions.PeriodSeconds);
                AddParameter(providerCommand, "created_at", authenticator.CreatedAt);
                await providerCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<TotpAuthenticatorState?> GetAsync(
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
                       t.protected_secret,
                       t.algorithm,
                       t.digits,
                       t.period_seconds,
                       t.last_accepted_time_step
                FROM identity_access.user_authenticators AS u
                INNER JOIN identity_access.totp_authenticators AS t
                    ON t.identity_scope_id = u.identity_scope_id
                   AND t.authenticator_id = u.authenticator_id
                WHERE u.identity_scope_id = @scope
                  AND u.user_id = @user_id
                  AND u.authenticator_id = @authenticator_id
                  AND u.provider_key = @provider;
                """;
            AddParameter(command, "scope", identityScopeId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "authenticator_id", authenticatorId);
            AddParameter(command, "provider", TotpAuthenticationFactorProviderKey.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            return new TotpAuthenticatorState(
                (UserAuthenticatorStatus)reader.GetInt16(0),
                reader.GetFieldValue<byte[]>(1),
                reader.GetString(2),
                reader.GetInt16(3),
                reader.GetInt16(4),
                reader.IsDBNull(5) ? null : reader.GetInt64(5));
        }

        public Task<TotpStoreMutationResult> TryConfirmAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            long acceptedTimeStep,
            DateTimeOffset confirmedAt,
            CancellationToken cancellationToken) =>
            MutateAcceptedTimeStepAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                acceptedTimeStep,
                confirmedAt,
                requirePending: true,
                cancellationToken: cancellationToken);

        public Task<TotpStoreMutationResult> TryRecordSuccessfulVerificationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            long acceptedTimeStep,
            DateTimeOffset usedAt,
            CancellationToken cancellationToken) =>
            MutateAcceptedTimeStepAsync(
                route,
                identityScopeId,
                userId,
                authenticatorId,
                acceptedTimeStep,
                usedAt,
                requirePending: false,
                cancellationToken: cancellationToken);

        private async Task<TotpStoreMutationResult> MutateAcceptedTimeStepAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            long acceptedTimeStep,
            DateTimeOffset occurredAt,
            bool requirePending,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            ValidateIdentifiers(identityScopeId, userId, authenticatorId);
            if (acceptedTimeStep < 0) throw new ArgumentOutOfRangeException(nameof(acceptedTimeStep));

            await using var connection = await _connectionFactory.OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            short status;
            long? lastAcceptedTimeStep;

            await using (var lockCommand = connection.CreateCommand())
            {
                lockCommand.Transaction = transaction;
                lockCommand.CommandText = """
                    SELECT u.status, t.last_accepted_time_step
                    FROM identity_access.user_authenticators AS u
                    INNER JOIN identity_access.totp_authenticators AS t
                        ON t.identity_scope_id = u.identity_scope_id
                       AND t.authenticator_id = u.authenticator_id
                    WHERE u.identity_scope_id = @scope
                      AND u.user_id = @user_id
                      AND u.authenticator_id = @authenticator_id
                      AND u.provider_key = @provider
                    FOR UPDATE OF u, t;
                    """;
                AddParameter(lockCommand, "scope", identityScopeId);
                AddParameter(lockCommand, "user_id", userId);
                AddParameter(lockCommand, "authenticator_id", authenticatorId);
                AddParameter(lockCommand, "provider", TotpAuthenticationFactorProviderKey.Value);

                await using var reader = await lockCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return TotpStoreMutationResult.NotFound;
                }

                status = reader.GetInt16(0);
                lastAcceptedTimeStep = reader.IsDBNull(1) ? null : reader.GetInt64(1);
            }

            var expectedStatus = requirePending
                ? UserAuthenticatorStatus.Pending
                : UserAuthenticatorStatus.Active;

            if (status != (short)expectedStatus)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return TotpStoreMutationResult.InvalidState;
            }

            if (lastAcceptedTimeStep is not null && acceptedTimeStep <= lastAcceptedTimeStep.Value)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return TotpStoreMutationResult.ReplayDetected;
            }

            await using (var genericCommand = connection.CreateCommand())
            {
                genericCommand.Transaction = transaction;
                genericCommand.CommandText = requirePending
                    ? """
                      UPDATE identity_access.user_authenticators
                      SET status = @active_status,
                          confirmed_at = @occurred_at,
                          last_used_at = @occurred_at,
                          row_version = row_version + 1,
                          updated_at = transaction_timestamp()
                      WHERE identity_scope_id = @scope
                        AND user_id = @user_id
                        AND authenticator_id = @authenticator_id
                        AND provider_key = @provider;
                      """
                    : """
                      UPDATE identity_access.user_authenticators
                      SET last_used_at = @occurred_at,
                          row_version = row_version + 1,
                          updated_at = transaction_timestamp()
                      WHERE identity_scope_id = @scope
                        AND user_id = @user_id
                        AND authenticator_id = @authenticator_id
                        AND provider_key = @provider;
                      """;
                if (requirePending)
                {
                    AddParameter(genericCommand, "active_status", (short)UserAuthenticatorStatus.Active);
                }
                AddParameter(genericCommand, "occurred_at", occurredAt);
                AddParameter(genericCommand, "scope", identityScopeId);
                AddParameter(genericCommand, "user_id", userId);
                AddParameter(genericCommand, "authenticator_id", authenticatorId);
                AddParameter(genericCommand, "provider", TotpAuthenticationFactorProviderKey.Value);
                await genericCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var providerCommand = connection.CreateCommand())
            {
                providerCommand.Transaction = transaction;
                providerCommand.CommandText = """
                    UPDATE identity_access.totp_authenticators
                    SET last_accepted_time_step = @time_step,
                        row_version = row_version + 1,
                        updated_at = transaction_timestamp()
                    WHERE identity_scope_id = @scope
                      AND authenticator_id = @authenticator_id;
                    """;
                AddParameter(providerCommand, "time_step", acceptedTimeStep);
                AddParameter(providerCommand, "scope", identityScopeId);
                AddParameter(providerCommand, "authenticator_id", authenticatorId);
                await providerCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return TotpStoreMutationResult.Succeeded;
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
