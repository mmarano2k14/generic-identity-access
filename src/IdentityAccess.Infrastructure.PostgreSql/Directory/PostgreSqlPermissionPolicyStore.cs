using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL permission policy.</summary>
    internal sealed class PostgreSqlPermissionPolicyStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IPermissionPolicyStore
    {
        /// <summary>Gets the requested permission policy record from the resolved database route.</summary>
        public async Task<VersionedRecord<PermissionPolicy>?> GetAsync(ResolvedDatabaseRoute route,
            PermissionPolicyReference policy, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT display_name, status, row_version
                FROM identity_access.permission_policies
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND policy_id = @policy_id;
                """, connection);
            AddIdentity(command, policy);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            var value = new PermissionPolicy(policy, reader.GetString(0), (PolicyStatus)reader.GetInt16(1));
            return new VersionedRecord<PermissionPolicy>(value, reader.GetInt64(2));
        }

        /// <summary>Creates a permission policy record in the resolved database route.</summary>
        public async Task<VersionedRecord<PermissionPolicy>> CreateAsync(ResolvedDatabaseRoute route,
            PermissionPolicy policy, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.Reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.permission_policies
                    (identity_scope_id, tenant_id, application_key, policy_id, display_name, status)
                VALUES (@scope, @tenant_id, @application_key, @policy_id, @display_name, @status)
                RETURNING row_version;
                """, connection);
            AddIdentity(command, policy.Reference);
            command.Parameters.AddWithValue("display_name", policy.DisplayName);
            command.Parameters.AddWithValue("status", (short)policy.Status);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted policy did not return a row version."));
            return new VersionedRecord<PermissionPolicy>(policy, version);
        }

        /// <summary>Updates a permission policy record using optimistic concurrency.</summary>
        public async Task<VersionedRecord<PermissionPolicy>> UpdateAsync(ResolvedDatabaseRoute route,
            PermissionPolicy policy, long expectedVersion, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(policy);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            PostgreSqlDirectoryGuard.EnsureScope(route, policy.Reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.permission_policies
                SET display_name = @display_name,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
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
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<PermissionPolicy>(policy, (long)result);
        }

        private static void AddIdentity(NpgsqlCommand command, PermissionPolicyReference policy)
        {
            command.Parameters.AddWithValue("scope", policy.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", policy.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", policy.Application.Value);
            command.Parameters.AddWithValue("policy_id", policy.PolicyId);
        }
    }
}
