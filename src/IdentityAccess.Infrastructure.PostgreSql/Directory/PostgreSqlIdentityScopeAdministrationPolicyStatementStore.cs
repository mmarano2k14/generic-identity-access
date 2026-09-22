using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Persists identity-scope administration policy statements in PostgreSQL.</summary>
    internal sealed class PostgreSqlIdentityScopeAdministrationPolicyStatementStore(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IIdentityScopeAdministrationPolicyStatementStore
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<IdentityScopeAdministrationPolicyStatement>> ListAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyReference policy,
            CancellationToken cancellationToken)
        {
            Ensure(route, policy);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT statement_id, model_version,
                       capability_resource, capability_feature, capability_action
                FROM identity_access.identity_scope_administration_policy_statements
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                ORDER BY statement_id;
                """, connection);

            AddPolicy(command, policy);
            var result = new List<IdentityScopeAdministrationPolicyStatement>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                result.Add(
                    new IdentityScopeAdministrationPolicyStatement(
                        reader.GetGuid(0),
                        policy,
                        new ApplicationSecurityModelReference(
                            policy.IdentityScopeId,
                            policy.Application,
                            reader.GetInt32(1)),
                        new CapabilityPattern(
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetString(4))));
            }

            return result.AsReadOnly();
        }

        /// <inheritdoc />
        public async Task AddAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyStatement statement,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(statement);
            Ensure(route, statement.Policy);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.identity_scope_administration_policy_statements
                    (identity_scope_id, application_key, policy_id, statement_id, model_version,
                     capability_resource, capability_feature, capability_action)
                VALUES
                    (@scope, @application_key, @policy_id, @statement_id, @model_version,
                     @capability_resource, @capability_feature, @capability_action);
                """, connection);

            AddPolicy(command, statement.Policy);
            command.Parameters.AddWithValue("statement_id", statement.StatementId);
            command.Parameters.AddWithValue("model_version", statement.Model.Version);
            command.Parameters.AddWithValue("capability_resource", statement.Pattern.Resource);
            command.Parameters.AddWithValue("capability_feature", statement.Pattern.Feature);
            command.Parameters.AddWithValue("capability_action", statement.Pattern.Action);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyReference policy,
            Guid statementId,
            CancellationToken cancellationToken)
        {
            Ensure(route, policy);

            if (statementId == Guid.Empty)
                throw new ArgumentException("Statement id must not be empty.", nameof(statementId));

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.identity_scope_administration_policy_statements
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND statement_id = @statement_id;
                """, connection);

            AddPolicy(command, policy);
            command.Parameters.AddWithValue("statement_id", statementId);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
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

        private static void AddPolicy(
            NpgsqlCommand command,
            IdentityScopeAdministrationPolicyReference policy)
        {
            command.Parameters.AddWithValue("scope", policy.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", policy.PolicyId);
        }
    }
}
