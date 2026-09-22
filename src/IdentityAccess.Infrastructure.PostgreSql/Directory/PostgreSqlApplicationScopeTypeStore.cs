using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;
using NpgsqlTypes;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL application scope type.</summary>
    internal sealed class PostgreSqlApplicationScopeTypeStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IApplicationScopeTypeStore
    {
        /// <summary>Adds a application scope type record to the resolved database route.</summary>
        public async Task AddAsync(ResolvedDatabaseRoute route, ApplicationScopeTypeDefinition definition,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(definition);
            PostgreSqlDirectoryGuard.EnsureScope(route, definition.Model.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.application_scope_types
                    (identity_scope_id, application_key, model_version, scope_type_key, display_name,
                     parent_scope_type_key, can_attach_to_tenant)
                VALUES (@scope, @application_key, @model_version, @scope_type_key, @display_name,
                        @parent_scope_type_key, @can_attach_to_tenant);
                """, connection);
            command.Parameters.AddWithValue("scope", definition.Model.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", definition.Model.Application.Value);
            command.Parameters.AddWithValue("model_version", definition.Model.Version);
            command.Parameters.AddWithValue("scope_type_key", definition.Type.Value);
            command.Parameters.AddWithValue("display_name", definition.DisplayName);
            var parentType = command.Parameters.Add("parent_scope_type_key", NpgsqlDbType.Varchar);
            parentType.Value = (object?)definition.ParentType?.Value ?? DBNull.Value;
            command.Parameters.AddWithValue("can_attach_to_tenant", definition.CanAttachToTenant);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Lists application scope type records for the supplied scope.</summary>
        public async Task<IReadOnlyList<ApplicationScopeTypeDefinition>> ListAsync(ResolvedDatabaseRoute route,
            ApplicationSecurityModelReference model, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(model);
            PostgreSqlDirectoryGuard.EnsureScope(route, model.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT scope_type_key, display_name, parent_scope_type_key, can_attach_to_tenant
                FROM identity_access.application_scope_types
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND model_version = @model_version
                ORDER BY scope_type_key;
                """, connection);
            command.Parameters.AddWithValue("scope", model.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", model.Application.Value);
            command.Parameters.AddWithValue("model_version", model.Version);
            var result = new List<ApplicationScopeTypeDefinition>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var parent = reader.IsDBNull(2) ? null : new ResourceScopeTypeKey(reader.GetString(2));
                result.Add(new ApplicationScopeTypeDefinition(model, new ResourceScopeTypeKey(reader.GetString(0)),
                    reader.GetString(1), parent, reader.GetBoolean(3)));
            }
            return result.AsReadOnly();
        }
    }
}
