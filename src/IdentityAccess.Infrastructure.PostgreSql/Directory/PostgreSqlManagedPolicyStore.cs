using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>PostgreSQL persistence for reusable managed policy metadata.</summary>
    internal sealed class PostgreSqlManagedPolicyStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IManagedPolicyStore
    {
        /// <inheritdoc />
        public async Task<VersionedRecord<ManagedPolicy>?> GetAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyReference policy,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT policy_key, display_name, status, default_version, row_version
                FROM identity_access.managed_policies
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id;
                """, connection);
            AddIdentity(command, policy);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            return Read(policy, reader);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            ArgumentNullException.ThrowIfNull(application);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT policy_id, policy_key, display_name, status, default_version, row_version
                FROM identity_access.managed_policies
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND (@search_pattern IS NULL
                       OR lower(display_name) LIKE @search_pattern
                       OR lower(policy_key) LIKE @search_pattern
                       OR policy_id = @search_id)
                ORDER BY policy_key, policy_id
                LIMIT @limit OFFSET @offset;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            command.Parameters.AddWithValue("application_key", application.Value);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("limit", limit);
            command.Parameters.AddWithValue("offset", offset);
            var records = new List<VersionedRecord<ManagedPolicy>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var reference = new ManagedPolicyReference(identityScopeId, application, reader.GetGuid(0));
                records.Add(Read(reference, reader, firstColumnOffset: 1));
            }
            return records;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VersionedRecord<ManagedPolicy>>> ListAttachableAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            string? search,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            ArgumentNullException.ThrowIfNull(application);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT p.policy_id, p.policy_key, p.display_name, p.status, p.default_version, p.row_version
                FROM identity_access.managed_policies AS p
                INNER JOIN identity_access.managed_policy_versions AS pv
                    ON pv.identity_scope_id = p.identity_scope_id
                   AND pv.application_key = p.application_key
                   AND pv.policy_id = p.policy_id
                   AND pv.policy_version = p.default_version
                WHERE p.identity_scope_id = @scope
                  AND p.application_key = @application_key
                  AND p.status = @active_policy_status
                  AND p.default_version IS NOT NULL
                  AND pv.published_at IS NOT NULL
                  AND (@search_pattern IS NULL
                       OR lower(p.display_name) LIKE @search_pattern
                       OR lower(p.policy_key) LIKE @search_pattern
                       OR p.policy_id = @search_id)
                ORDER BY p.policy_key, p.policy_id
                LIMIT @limit OFFSET @offset;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            command.Parameters.AddWithValue("application_key", application.Value);
            command.Parameters.AddWithValue("active_policy_status", (short)PolicyStatus.Active);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("limit", limit);
            command.Parameters.AddWithValue("offset", offset);
            var records = new List<VersionedRecord<ManagedPolicy>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var reference = new ManagedPolicyReference(identityScopeId, application, reader.GetGuid(0));
                records.Add(Read(reference, reader, firstColumnOffset: 1));
            }
            return records;
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<ManagedPolicy>> CreateAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicy policy,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            if (policy.DefaultVersion is not null)
                throw new InvalidOperationException("A new managed policy cannot select a default version before that version exists.");
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.Reference.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.managed_policies
                    (identity_scope_id, application_key, policy_id, policy_key, display_name, status)
                VALUES (@scope, @application_key, @policy_id, @policy_key, @display_name, @status)
                RETURNING row_version;
                """, connection);
            AddIdentity(command, policy.Reference);
            command.Parameters.AddWithValue("policy_key", policy.Key.Value);
            command.Parameters.AddWithValue("display_name", policy.DisplayName);
            command.Parameters.AddWithValue("status", (short)policy.Status);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted managed policy did not return a row version."));
            return new VersionedRecord<ManagedPolicy>(policy, version);
        }

        /// <inheritdoc />
        public async Task<VersionedRecord<ManagedPolicy>> UpdateAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicy policy,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.Reference.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.managed_policies
                SET policy_key = @policy_key,
                    display_name = @display_name,
                    status = @status,
                    default_version = @default_version,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection);
            AddIdentity(command, policy.Reference);
            command.Parameters.AddWithValue("policy_key", policy.Key.Value);
            command.Parameters.AddWithValue("display_name", policy.DisplayName);
            command.Parameters.AddWithValue("status", (short)policy.Status);
            command.Parameters.AddWithValue("default_version", (object?)policy.DefaultVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("expected_version", expectedVersion);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<ManagedPolicy>(policy, (long)result);
        }

        private static VersionedRecord<ManagedPolicy> Read(
            ManagedPolicyReference reference,
            NpgsqlDataReader reader,
            int firstColumnOffset = 0)
        {
            var defaultVersionOrdinal = firstColumnOffset + 3;
            var value = new ManagedPolicy(
                reference,
                new ManagedPolicyKey(reader.GetString(firstColumnOffset)),
                reader.GetString(firstColumnOffset + 1),
                (PolicyStatus)reader.GetInt16(firstColumnOffset + 2),
                reader.IsDBNull(defaultVersionOrdinal) ? null : reader.GetInt32(defaultVersionOrdinal));
            return new VersionedRecord<ManagedPolicy>(value, reader.GetInt64(firstColumnOffset + 4));
        }

        private static void AddIdentity(NpgsqlCommand command, ManagedPolicyReference policy)
        {
            command.Parameters.AddWithValue("scope", policy.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", policy.PolicyId);
        }
    }
}
