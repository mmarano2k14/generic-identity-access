using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL tenant directory.</summary>
    internal sealed class PostgreSqlTenantDirectoryStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : ITenantDirectoryStore
    {
        /// <summary>Gets the requested tenant record from the resolved database route.</summary>
        public async Task<VersionedRecord<Tenant>?> GetAsync(ResolvedDatabaseRoute route, TenantReference tenant,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT display_name, status, row_version
                FROM identity_access.tenants
                WHERE identity_scope_id = @scope AND tenant_id = @tenant_id;
                """, connection);
            command.Parameters.AddWithValue("scope", tenant.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.TenantId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            var value = new Tenant(tenant, reader.GetString(0), (TenantStatus)reader.GetInt16(1));
            return new VersionedRecord<Tenant>(value, reader.GetInt64(2));
        }

        /// <summary>Lists a bounded window of tenant records in the resolved database route.</summary>
        public async Task<IReadOnlyList<VersionedRecord<Tenant>>> ListAsync(ResolvedDatabaseRoute route,
            Guid identityScopeId, string? search, int offset, int limit, CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT tenant_id, display_name, status, row_version
                FROM identity_access.tenants
                WHERE identity_scope_id = @scope
                  AND (@search_pattern IS NULL
                       OR lower(display_name) LIKE @search_pattern
                       OR tenant_id = @search_id)
                ORDER BY tenant_id
                LIMIT @limit OFFSET @offset;
                """, connection);
            command.Parameters.AddWithValue("scope", identityScopeId);
            PostgreSqlAdministrationSearch.AddParameters(command, search);
            command.Parameters.AddWithValue("limit", limit);
            command.Parameters.AddWithValue("offset", offset);
            var records = new List<VersionedRecord<Tenant>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var reference = new TenantReference(identityScopeId, reader.GetGuid(0));
                var tenant = new Tenant(reference, reader.GetString(1), (TenantStatus)reader.GetInt16(2));
                records.Add(new VersionedRecord<Tenant>(tenant, reader.GetInt64(3)));
            }
            return records;
        }

        /// <summary>Creates a tenant record in the resolved database route.</summary>
        public async Task<VersionedRecord<Tenant>> CreateAsync(ResolvedDatabaseRoute route, Tenant tenant,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.Reference.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.tenants(identity_scope_id, tenant_id, display_name, status)
                VALUES (@scope, @tenant_id, @display_name, @status)
                RETURNING row_version;
                """, connection);
            command.Parameters.AddWithValue("scope", tenant.Reference.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.Reference.TenantId);
            command.Parameters.AddWithValue("display_name", tenant.DisplayName);
            command.Parameters.AddWithValue("status", (short)tenant.Status);
            var version = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The inserted tenant did not return a row version."));
            return new VersionedRecord<Tenant>(tenant, version);
        }

        /// <summary>Updates a tenant record using optimistic concurrency.</summary>
        public async Task<VersionedRecord<Tenant>> UpdateAsync(ResolvedDatabaseRoute route, Tenant tenant,
            long expectedVersion, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            PostgreSqlDirectoryGuard.EnsureVersion(expectedVersion);
            PostgreSqlDirectoryGuard.EnsureScope(route, tenant.Reference.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                UPDATE identity_access.tenants
                SET display_name = @display_name,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = transaction_timestamp()
                WHERE identity_scope_id = @scope
                  AND tenant_id = @tenant_id
                  AND row_version = @expected_version
                RETURNING row_version;
                """, connection);
            command.Parameters.AddWithValue("scope", tenant.Reference.IdentityScopeId);
            command.Parameters.AddWithValue("tenant_id", tenant.Reference.TenantId);
            command.Parameters.AddWithValue("display_name", tenant.DisplayName);
            command.Parameters.AddWithValue("status", (short)tenant.Status);
            command.Parameters.AddWithValue("expected_version", expectedVersion);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new IdentityConcurrencyException();
            return new VersionedRecord<Tenant>(tenant, (long)result);
        }
    }
}
