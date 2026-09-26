using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Persists identity-scope administration policies in PostgreSQL.</summary>
    internal sealed class PostgreSqlIdentityScopeAdministrationPolicyStore(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IIdentityScopeAdministrationPolicyStore
    {
        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationPolicy>?> GetAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyReference policy,
            CancellationToken cancellationToken)
        {
            Ensure(route, policy);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT display_name, status, row_version
                FROM identity_access.identity_scope_administration_policies
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id;
                """, connection);

            AddIdentity(command, policy);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            return new VersionedRecord<IdentityScopeAdministrationPolicy>(
                new IdentityScopeAdministrationPolicy(
                    policy,
                    reader.GetString(0),
                    (PolicyStatus)reader.GetInt16(1)),
                reader.GetInt64(2));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VersionedRecord<IdentityScopeAdministrationPolicy>>> ListAsync(
            ResolvedDatabaseRoute route, Guid identityScopeId, ApplicationKey application, string? search, int offset, int limit,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            if (route.Request.Application != application)
                throw new InvalidOperationException("Administration policy application does not match the route.");

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT policy_id, display_name, status, row_version
                FROM identity_access.identity_scope_administration_policies
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND (@search_pattern IS NULL
                       OR lower(display_name) LIKE @search_pattern
                       OR policy_id = @search_id)
                ORDER BY policy_id
                OFFSET @offset
                LIMIT @limit;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            command.Parameters.AddWithValue("application_key", application.Value);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("offset", offset);
            command.Parameters.AddWithValue("limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var records = new List<VersionedRecord<IdentityScopeAdministrationPolicy>>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var reference = new IdentityScopeAdministrationPolicyReference(identityScopeId, application, reader.GetGuid(0));
                records.Add(new VersionedRecord<IdentityScopeAdministrationPolicy>(
                    new IdentityScopeAdministrationPolicy(reference, reader.GetString(1), (PolicyStatus)reader.GetInt16(2)),
                    reader.GetInt64(3)));
            }

            return records;
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationPolicy>> CreateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicy policy,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            Ensure(route, policy.Reference);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.identity_scope_administration_policies
                    (identity_scope_id, application_key, policy_id, display_name, status)
                VALUES
                    (@scope, @application_key, @policy_id, @display_name, @status)
                RETURNING row_version;
                """, connection);

            AddIdentity(command, policy.Reference);
            command.Parameters.AddWithValue("display_name", policy.DisplayName);
            command.Parameters.AddWithValue("status", (short)policy.Status);

            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Inserted administration policy returned no version."));

            return new VersionedRecord<IdentityScopeAdministrationPolicy>(policy, version);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<IdentityScopeAdministrationPolicy>> UpdateAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicy policy,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            Ensure(route, policy.Reference);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.identity_scope_administration_policies
                SET display_name = @display_name,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection);

            AddIdentity(command, policy.Reference);
            command.Parameters.AddWithValue("display_name", policy.DisplayName);
            command.Parameters.AddWithValue("status", (short)policy.Status);
            command.Parameters.AddWithValue("expected_version", expectedVersion);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull)
                throw new IdentityConcurrencyException();

            return new VersionedRecord<IdentityScopeAdministrationPolicy>(policy, (long)result);
        }

        private static void Ensure(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyReference policy)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.IdentityScopeId);
            if (route.Request.Application != policy.Application)
                throw new InvalidOperationException("Administration policy application does not match the route.");
        }

        private static void AddIdentity(
            NpgsqlCommand command,
            IdentityScopeAdministrationPolicyReference policy)
        {
            command.Parameters.AddWithValue("scope", policy.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", policy.PolicyId);
        }
    }
}
