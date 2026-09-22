using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL policy statement.</summary>
    internal sealed class PostgreSqlPolicyStatementStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IPolicyStatementStore
    {
        /// <summary>Adds a policy statement record to the resolved database route.</summary>
        public async Task AddAsync(ResolvedDatabaseRoute route, PolicyStatement statement,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(statement);
            PostgreSqlDirectoryGuard.EnsureScope(route, statement.Policy.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.policy_statements
                    (identity_scope_id, tenant_id, application_key, policy_id, statement_id,
                     model_version, capability_resource, capability_feature, capability_action)
                VALUES (@scope, @tenant_id, @application_key, @policy_id, @statement_id,
                        @model_version, @capability_resource, @capability_feature, @capability_action);
                """, connection);
            AddIdentity(command, statement.Policy);
            command.Parameters.AddWithValue("statement_id", statement.StatementId);
            command.Parameters.AddWithValue("model_version", statement.Model.Version);
            AddPattern(command, statement.Pattern);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Removes a policy statement record from the resolved database route.</summary>
        public async Task<bool> RemoveAsync(ResolvedDatabaseRoute route, PermissionPolicyReference policy,
            Guid statementId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            if (statementId == Guid.Empty) throw new ArgumentException("Statement id must not be empty.", nameof(statementId));
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.policy_statements
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                  AND statement_id = @statement_id;
                """, connection);
            AddIdentity(command, policy);
            command.Parameters.AddWithValue("statement_id", statementId);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }

        /// <summary>Lists policy statement records for the supplied scope.</summary>
        public async Task<IReadOnlyList<PolicyStatement>> ListAsync(ResolvedDatabaseRoute route,
            PermissionPolicyReference policy, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT statement_id, model_version,
                       capability_resource, capability_feature, capability_action
                FROM identity_access.policy_statements
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND policy_id = @policy_id
                ORDER BY statement_id;
                """, connection);
            AddIdentity(command, policy);
            var result = new List<PolicyStatement>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var model = new ApplicationSecurityModelReference(policy.Tenant.IdentityScopeId,
                    policy.Application, reader.GetInt32(1));
                result.Add(new PolicyStatement(reader.GetGuid(0), policy, model,
                    new CapabilityPattern(reader.GetString(2), reader.GetString(3), reader.GetString(4))));
            }
            return result.AsReadOnly();
        }

        private static void AddIdentity(NpgsqlCommand command, PermissionPolicyReference policy)
        {
            command.Parameters.AddWithValue("scope", policy.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", policy.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", policy.PolicyId);
        }

        private static void AddPattern(NpgsqlCommand command, CapabilityPattern pattern)
        {
            command.Parameters.AddWithValue("capability_resource", pattern.Resource);
            command.Parameters.AddWithValue("capability_feature", pattern.Feature);
            command.Parameters.AddWithValue("capability_action", pattern.Action);
        }
    }
}
