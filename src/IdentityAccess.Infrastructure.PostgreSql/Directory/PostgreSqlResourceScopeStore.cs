using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL resource scope.</summary>
    internal sealed class PostgreSqlResourceScopeStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IResourceScopeStore
    {
        /// <summary>Gets the requested resource scope record from the resolved database route.</summary>
        public async Task<VersionedRecord<ResourceScope>?> GetAsync(ResolvedDatabaseRoute route,
            ResourceScopeReference reference, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            PostgreSqlDirectoryGuard.EnsureScope(route, reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT scope_model_version, scope_type_key, external_resource_id, display_name,
                       parent_resource_scope_id, status, row_version
                FROM identity_access.resource_scopes
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND resource_scope_id = @resource_scope_id;
                """, connection);
            AddIdentity(command, reference);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            return Read(reference, reader);
        }

        /// <summary>Lists resource scope records for the supplied scope.</summary>
        public async Task<IReadOnlyList<VersionedRecord<ResourceScope>>> ListAsync(ResolvedDatabaseRoute route,
            TenantReference tenant, ApplicationKey application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(application);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT resource_scope_id, scope_model_version, scope_type_key, external_resource_id, display_name,
                       parent_resource_scope_id, status, row_version
                FROM identity_access.resource_scopes
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                ORDER BY scope_type_key, external_resource_id, resource_scope_id;
                """, connection);
            command.Parameters.AddWithValue("scope", tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.TenantId);
            command.Parameters.AddWithValue("application_key", application.Value);
            var result = new List<VersionedRecord<ResourceScope>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var reference = new ResourceScopeReference(tenant, application, reader.GetGuid(0));
                result.Add(Read(reference, reader, 1));
            }
            return result.AsReadOnly();
        }

        /// <summary>Creates a resource scope record in the resolved database route.</summary>
        public async Task<VersionedRecord<ResourceScope>> CreateAsync(ResolvedDatabaseRoute route, ResourceScope scope,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scope);
            PostgreSqlDirectoryGuard.EnsureScope(route, scope.Reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.resource_scopes
                    (identity_scope_id, tenant_id, application_key, resource_scope_id,
                     scope_model_version, scope_type_key, external_resource_id, display_name,
                     parent_resource_scope_id, status)
                VALUES (@scope, @tenant_id, @application_key, @resource_scope_id,
                        @scope_model_version, @scope_type_key, @external_resource_id, @display_name,
                        @parent_resource_scope_id, @status)
                RETURNING row_version;
                """, connection);
            AddIdentity(command, scope.Reference);
            AddValue(command, scope);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted resource scope did not return a row version."));
            return new VersionedRecord<ResourceScope>(scope, version);
        }

        /// <summary>Updates a resource scope record using optimistic concurrency.</summary>
        public async Task<VersionedRecord<ResourceScope>> UpdateAsync(ResolvedDatabaseRoute route, ResourceScope scope,
            long expectedVersion, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(scope);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            PostgreSqlDirectoryGuard.EnsureScope(route, scope.Reference.Tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.resource_scopes
                SET scope_model_version = @scope_model_version,
                    scope_type_key = @scope_type_key,
                    external_resource_id = @external_resource_id,
                    display_name = @display_name,
                    parent_resource_scope_id = @parent_resource_scope_id,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND resource_scope_id = @resource_scope_id
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection);
            AddIdentity(command, scope.Reference);
            AddValue(command, scope);
            command.Parameters.AddWithValue("expected_version", expectedVersion);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<ResourceScope>(scope, (long)result);
        }

        private static VersionedRecord<ResourceScope> Read(ResourceScopeReference reference, NpgsqlDataReader reader,
            int offset = 0)
        {
            var value = new ResourceScope(reference,
                reader.GetInt32(offset + 0),
                new ResourceScopeTypeKey(reader.GetString(offset + 1)),
                reader.GetString(offset + 2),
                reader.GetString(offset + 3),
                reader.IsDBNull(offset + 4) ? null : reader.GetGuid(offset + 4),
                (ResourceScopeStatus)reader.GetInt16(offset + 5));
            return new VersionedRecord<ResourceScope>(value, reader.GetInt64(offset + 6));
        }

        private static void AddIdentity(NpgsqlCommand command, ResourceScopeReference reference)
        {
            command.Parameters.AddWithValue("scope", reference.Tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", reference.Tenant.TenantId);
            command.Parameters.AddWithValue("application_key", reference.Application.Value);
            command.Parameters.AddWithValue("resource_scope_id", reference.ResourceScopeId);
        }

        private static void AddValue(NpgsqlCommand command, ResourceScope scope)
        {
            command.Parameters.AddWithValue("scope_model_version", scope.ModelVersion);
            command.Parameters.AddWithValue("scope_type_key", scope.Type.Value);
            command.Parameters.AddWithValue("external_resource_id", scope.ExternalResourceId);
            command.Parameters.AddWithValue("display_name", scope.DisplayName);
            var parent = command.Parameters.Add("parent_resource_scope_id", NpgsqlDbType.Uuid);
            parent.Value = (object?)scope.ParentResourceScopeId ?? DBNull.Value;
            command.Parameters.AddWithValue("status", (short)scope.Status);
        }
    }
}
