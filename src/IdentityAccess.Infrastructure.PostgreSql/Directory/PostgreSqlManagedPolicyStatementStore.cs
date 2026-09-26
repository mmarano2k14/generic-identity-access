using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>PostgreSQL persistence for managed-policy statements.</summary>
    internal sealed class PostgreSqlManagedPolicyStatementStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IManagedPolicyStatementStore
    {
        /// <inheritdoc />
        public async Task AddAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyStatement statement,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(statement);
            PostgreSqlDirectoryGuard.EnsureScope(route, statement.PolicyVersion.Policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.managed_policy_statements
                    (identity_scope_id, application_key, policy_id, policy_version, model_version,
                     statement_id, capability_resource, capability_feature, capability_action)
                SELECT @scope, @application_key, @policy_id, @policy_version, @model_version,
                       @statement_id, @capability_resource, @capability_feature, @capability_action
                WHERE EXISTS
                (
                    SELECT 1
                    FROM identity_access.managed_policy_versions AS pv
                    WHERE pv.identity_scope_id = @scope
                      AND pv.application_key = @application_key
                      AND pv.policy_id = @policy_id
                      AND pv.policy_version = @policy_version
                      AND pv.published_at IS NULL
                )
                RETURNING 1;
                """, connection);
            AddIdentity(command, statement.PolicyVersion);
            command.Parameters.AddWithValue("model_version", statement.Model.Version);
            command.Parameters.AddWithValue("statement_id", statement.StatementId);
            AddPattern(command, statement.Pattern);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull)
                throw new InvalidOperationException("Managed policy statements can be added only to unpublished policy versions.");
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersionReference policyVersion,
            Guid statementId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policyVersion);
            if (statementId == Guid.Empty) throw new ArgumentException("A statement identifier is required.", nameof(statementId));
            PostgreSqlDirectoryGuard.EnsureScope(route, policyVersion.Policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.managed_policy_statements
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND policy_version = @policy_version
                  AND statement_id = @statement_id
                  AND EXISTS
                  (
                      SELECT 1
                      FROM identity_access.managed_policy_versions AS pv
                      WHERE pv.identity_scope_id = @scope
                        AND pv.application_key = @application_key
                        AND pv.policy_id = @policy_id
                        AND pv.policy_version = @policy_version
                        AND pv.published_at IS NULL
                  );
                """, connection);
            AddIdentity(command, policyVersion);
            command.Parameters.AddWithValue("statement_id", statementId);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ManagedPolicyStatement>> ListAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersion version,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(version);
            PostgreSqlDirectoryGuard.EnsureScope(route, version.Reference.Policy.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT statement_id, model_version,
                       capability_resource, capability_feature, capability_action
                FROM identity_access.managed_policy_statements
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND policy_version = @policy_version
                ORDER BY statement_id;
                """, connection);
            AddIdentity(command, version.Reference);
            var statements = new List<ManagedPolicyStatement>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var model = new ApplicationSecurityModelReference(
                    version.Reference.Policy.IdentityScopeId,
                    version.Reference.Policy.Application,
                    reader.GetInt32(1));
                statements.Add(new ManagedPolicyStatement(
                    reader.GetGuid(0),
                    version.Reference,
                    model,
                    new CapabilityPattern(reader.GetString(2), reader.GetString(3), reader.GetString(4))));
            }
            return statements;
        }

        private static void AddIdentity(NpgsqlCommand command, ManagedPolicyVersionReference reference)
        {
            command.Parameters.AddWithValue("scope", reference.Policy.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", reference.Policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", reference.Policy.PolicyId);
            command.Parameters.AddWithValue("policy_version", reference.Version);
        }

        private static void AddPattern(NpgsqlCommand command, CapabilityPattern pattern)
        {
            command.Parameters.AddWithValue("capability_resource", pattern.Resource);
            command.Parameters.AddWithValue("capability_feature", pattern.Feature);
            command.Parameters.AddWithValue("capability_action", pattern.Action);
        }
    }
}
