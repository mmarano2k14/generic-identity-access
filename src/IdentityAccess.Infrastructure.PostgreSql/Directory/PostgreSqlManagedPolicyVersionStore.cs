using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>PostgreSQL persistence for versioned managed-policy definitions.</summary>
    internal sealed class PostgreSqlManagedPolicyVersionStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IManagedPolicyVersionStore
    {
        /// <inheritdoc />
        public async Task CreateAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersion version,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(version);
            if (version.IsPublished)
                throw new InvalidOperationException("A managed policy version must be created as an unpublished draft.");
            PostgreSqlDirectoryGuard.EnsureScope(route, version.Reference.Policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.managed_policy_versions
                    (identity_scope_id, application_key, policy_id, policy_version, model_version)
                VALUES (@scope, @application_key, @policy_id, @policy_version, @model_version);
                """, connection);
            AddIdentity(command, version.Reference);
            command.Parameters.AddWithValue("model_version", version.Model.Version);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ManagedPolicyVersion?> PublishAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersionReference reference,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            PostgreSqlDirectoryGuard.EnsureScope(route, reference.Policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.managed_policy_versions
                SET published_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND policy_version = @policy_version
                  AND published_at IS NULL
                RETURNING model_version, published_at;
                """, connection);
            AddIdentity(command, reference);
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return Create(reference, reader.GetInt32(0), reader.GetFieldValue<DateTimeOffset>(1));
            }

            await using var existing = new NpgsqlCommand("""
                SELECT model_version, published_at
                FROM identity_access.managed_policy_versions
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND policy_version = @policy_version;
                """, connection);
            AddIdentity(existing, reference);
            await using var existingReader = await existing.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await existingReader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            DateTimeOffset? publishedAt = existingReader.IsDBNull(1)
                ? null
                : existingReader.GetFieldValue<DateTimeOffset>(1);
            return Create(reference, existingReader.GetInt32(0), publishedAt);
        }

        /// <inheritdoc />
        public async Task<ManagedPolicyVersion?> GetAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersionReference reference,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            PostgreSqlDirectoryGuard.EnsureScope(route, reference.Policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT model_version, published_at
                FROM identity_access.managed_policy_versions
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND policy_version = @policy_version;
                """, connection);
            AddIdentity(command, reference);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            return Create(
                reference,
                reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetFieldValue<DateTimeOffset>(1));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ManagedPolicyVersion>> ListAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyReference policy,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT policy_version, model_version, published_at
                FROM identity_access.managed_policy_versions
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                ORDER BY policy_version;
                """, connection);
            command.Parameters.AddWithValue("scope", policy.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", policy.PolicyId);
            var versions = new List<ManagedPolicyVersion>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var reference = new ManagedPolicyVersionReference(policy, reader.GetInt32(0));
                versions.Add(Create(
                    reference,
                    reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2)));
            }
            return versions;
        }

        private static ManagedPolicyVersion Create(
            ManagedPolicyVersionReference reference,
            int modelVersion,
            DateTimeOffset? publishedAt) =>
            new(reference, new ApplicationSecurityModelReference(
                reference.Policy.IdentityScopeId,
                reference.Policy.Application,
                modelVersion), publishedAt);

        private static void AddIdentity(NpgsqlCommand command, ManagedPolicyVersionReference reference)
        {
            command.Parameters.AddWithValue("scope", reference.Policy.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", reference.Policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", reference.Policy.PolicyId);
            command.Parameters.AddWithValue("policy_version", reference.Version);
        }
    }
}
