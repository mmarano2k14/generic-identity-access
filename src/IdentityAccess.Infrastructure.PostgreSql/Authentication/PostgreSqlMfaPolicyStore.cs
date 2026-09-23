using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Authentication
{
    /// <summary>PostgreSQL persistence for provider-neutral MFA policy state.</summary>
    internal sealed class PostgreSqlMfaPolicyStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IMfaPolicyStore
    {
        /// <inheritdoc />
        public async Task<VersionedRecord<MfaPolicy>?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            await using var policyCommand = new NpgsqlCommand("""
                SELECT mode, row_version
                FROM identity_access.mfa_policies
                WHERE identity_scope_id = @scope
                  AND application_key = @application;
                """, connection);
            policyCommand.Parameters.AddWithValue("scope", identityScopeId);
            policyCommand.Parameters.AddWithValue("application", application.Value);

            short modeValue;
            long version;
            await using (var reader = await policyCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
                modeValue = reader.GetInt16(0);
                version = reader.GetInt64(1);
            }

            var providers = await ReadProvidersAsync(
                connection,
                identityScopeId,
                application,
                cancellationToken).ConfigureAwait(false);

            var policy = new MfaPolicy(
                identityScopeId,
                application,
                (MfaPolicyMode)modeValue,
                providers);

            return new VersionedRecord<MfaPolicy>(policy, version);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<MfaPolicy>> CreateAsync(
            ResolvedDatabaseRoute route,
            MfaPolicy policy,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.mfa_policies
                    (identity_scope_id, application_key, mode)
                VALUES
                    (@scope, @application, @mode)
                RETURNING row_version;
                """, connection, transaction);
            command.Parameters.AddWithValue("scope", policy.IdentityScopeId);
            command.Parameters.AddWithValue("application", policy.Application.Value);
            command.Parameters.AddWithValue("mode", (short)policy.Mode);

            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted MFA policy did not return a row version."));

            await ReplaceProvidersAsync(connection, transaction, policy, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new VersionedRecord<MfaPolicy>(policy, version);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<MfaPolicy>> UpdateAsync(
            ResolvedDatabaseRoute route,
            MfaPolicy policy,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.IdentityScopeId);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.mfa_policies
                SET mode = @mode,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND application_key = @application
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection, transaction);
            command.Parameters.AddWithValue("scope", policy.IdentityScopeId);
            command.Parameters.AddWithValue("application", policy.Application.Value);
            command.Parameters.AddWithValue("mode", (short)policy.Mode);
            command.Parameters.AddWithValue("expected_version", expectedVersion);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();

            await ReplaceProvidersAsync(connection, transaction, policy, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new VersionedRecord<MfaPolicy>(policy, (long)result);
        }

        private static async Task<IReadOnlyList<AuthenticationFactorProviderKey>> ReadProvidersAsync(
            NpgsqlConnection connection,
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT provider_key
                FROM identity_access.mfa_policy_providers
                WHERE identity_scope_id = @scope
                  AND application_key = @application
                ORDER BY provider_key;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            command.Parameters.AddWithValue("application", application.Value);

            var providers = new List<AuthenticationFactorProviderKey>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                providers.Add(new AuthenticationFactorProviderKey(reader.GetString(0)));
            }

            return providers;
        }

        private static async Task ReplaceProvidersAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            MfaPolicy policy,
            CancellationToken cancellationToken)
        {
            await using (var delete = new NpgsqlCommand("""
                DELETE FROM identity_access.mfa_policy_providers
                WHERE identity_scope_id = @scope
                  AND application_key = @application;
                """, connection, transaction))
            {
                delete.Parameters.AddWithValue("scope", policy.IdentityScopeId);
                delete.Parameters.AddWithValue("application", policy.Application.Value);
                await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var provider in policy.AllowedProviders)
            {
                await using var insert = new NpgsqlCommand("""
                    INSERT INTO identity_access.mfa_policy_providers
                        (identity_scope_id, application_key, provider_key)
                    VALUES
                        (@scope, @application, @provider);
                    """, connection, transaction);
                insert.Parameters.AddWithValue("scope", policy.IdentityScopeId);
                insert.Parameters.AddWithValue("application", policy.Application.Value);
                insert.Parameters.AddWithValue("provider", provider.Value);
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
