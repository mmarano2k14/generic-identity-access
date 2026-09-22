using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Persists identity-scope administration group-policy bindings in PostgreSQL.</summary>
    internal sealed class PostgreSqlIdentityScopeAdministrationBindingStore(
        IIdentityDatabaseConnectionFactory connectionFactory)
        : IIdentityScopeAdministrationBindingStore
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<IdentityScopeAdministrationGroupPolicyBinding>> ListAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group,
            CancellationToken cancellationToken)
        {
            Ensure(route, group);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT policy_id
                FROM identity_access.identity_scope_administration_group_policy_bindings
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND group_id = @group_id
                ORDER BY policy_id;
                """, connection);

            AddGroup(command, group);
            var result = new List<IdentityScopeAdministrationGroupPolicyBinding>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                result.Add(
                    new IdentityScopeAdministrationGroupPolicyBinding(
                        group,
                        new IdentityScopeAdministrationPolicyReference(
                            group.IdentityScopeId,
                            group.Application,
                            reader.GetGuid(0))));
            }

            return result.AsReadOnly();
        }

        /// <inheritdoc />
        public async Task<IdentityScopeAdministrationGroupPolicyBinding?> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupPolicyBinding binding,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(binding);
            Ensure(route, binding.Group);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                WITH eligible AS
                (
                    SELECT 1
                    FROM identity_access.identity_scope_administration_groups AS g
                    INNER JOIN identity_access.identity_scope_administration_policies AS p
                      ON p.identity_scope_id = g.identity_scope_id
                     AND p.application_key = g.application_key
                    WHERE g.identity_scope_id = @scope
                      AND g.application_key = @application_key
                      AND g.group_id = @group_id
                      AND g.status = @active_group_status
                      AND p.policy_id = @policy_id
                      AND p.status = @active_policy_status
                )
                INSERT INTO identity_access.identity_scope_administration_group_policy_bindings
                    (identity_scope_id, application_key, group_id, policy_id)
                SELECT @scope, @application_key, @group_id, @policy_id
                FROM eligible
                RETURNING 1;
                """, connection);

            AddGroup(command, binding.Group);
            command.Parameters.AddWithValue("policy_id", binding.Policy.PolicyId);
            command.Parameters.AddWithValue("active_group_status", (short)GroupStatus.Active);
            command.Parameters.AddWithValue("active_policy_status", (short)PolicyStatus.Active);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is null or DBNull ? null : binding;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupPolicyBinding binding,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(binding);
            Ensure(route, binding.Group);

            await using var connection = (NpgsqlConnection)await connectionFactory
                .OpenAsync(route, cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                DELETE FROM identity_access.identity_scope_administration_group_policy_bindings
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND group_id = @group_id
                  AND policy_id = @policy_id;
                """, connection);

            AddGroup(command, binding.Group);
            command.Parameters.AddWithValue("policy_id", binding.Policy.PolicyId);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }

        private static void Ensure(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group)
        {
            ArgumentNullException.ThrowIfNull(group);
            PostgreSqlDirectoryGuard.EnsureScope(route, group.IdentityScopeId);

            if (route.Request.Application != group.Application)
                throw new InvalidOperationException("Administration group application does not match the route.");
        }

        private static void AddGroup(
            NpgsqlCommand command,
            IdentityScopeAdministrationGroupReference group)
        {
            command.Parameters.AddWithValue("scope", group.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", group.Application.Value);
            command.Parameters.AddWithValue("group_id", group.GroupId);
        }
    }
}
