using Npgsql;
using NpgsqlTypes;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Infrastructure.PostgreSql
{
    /// <summary>Reads authoritative Identity Access ResourceScope state from the shared database.</summary>
    public sealed class PostgreSqlResourceScopeReferenceReader(
        NpgsqlDataSource dataSource) : IResourceScopeReferenceReader
    {
        /// <inheritdoc />
        public async Task<ResourceScopeReferenceState?> GetAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            await using var command = dataSource.CreateCommand("""
                SELECT
                    scope_model_version,
                    scope_type_key,
                    status
                FROM identity_access.resource_scopes
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND application_key = @application_key
                  AND resource_scope_id = @resource_scope_id;
                """);

            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                identityScopeId.Value);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                tenantId.Value);
            command.Parameters.AddWithValue(
                "application_key",
                NpgsqlDbType.Varchar,
                application.Value);
            command.Parameters.AddWithValue(
                "resource_scope_id",
                NpgsqlDbType.Uuid,
                resourceScopeId.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var reference = new ResourceScopeReference(
                identityScopeId,
                tenantId,
                application,
                resourceScopeId,
                new ScopeType(reader.GetString(1)),
                new SecurityModelVersion(reader.GetInt32(0)));

            return new ResourceScopeReferenceState(
                reference,
                IsActive: reader.GetInt16(2) == 1);
        }
    }
}
