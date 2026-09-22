using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{

    /// <summary>Provides persistence operations for PostgreSQL application security model.</summary>
    internal sealed class PostgreSqlApplicationSecurityModelStore(IIdentityDatabaseConnectionFactory connectionFactory)
        : IApplicationSecurityModelStore
    {
        /// <summary>Creates an application security model version.</summary>
        public async Task CreateModelAsync(ResolvedDatabaseRoute route, ApplicationSecurityModelReference model,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(model);
            PostgreSqlDirectoryGuard.EnsureScope(route, model.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.application_security_models
                    (identity_scope_id, application_key, model_version)
                VALUES (@scope, @application_key, @model_version);
                """, connection);
            AddModel(command, model);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Adds a capability to an application security model version.</summary>
        public async Task AddCapabilityAsync(ResolvedDatabaseRoute route, ApplicationCapability capability,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(capability);
            PostgreSqlDirectoryGuard.EnsureScope(route, capability.Model.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.application_capabilities
                    (identity_scope_id, application_key, model_version,
                     capability_resource, capability_feature, capability_action, display_name)
                VALUES (@scope, @application_key, @model_version,
                        @capability_resource, @capability_feature, @capability_action, @display_name);
                """, connection);
            AddModel(command, capability.Model);
            AddCapability(command, capability.Key);
            command.Parameters.AddWithValue("display_name", capability.DisplayName);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Lists capabilities declared by an application security model version.</summary>
        public async Task<IReadOnlyList<ApplicationCapability>> ListCapabilitiesAsync(ResolvedDatabaseRoute route,
            ApplicationSecurityModelReference model, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(model);
            PostgreSqlDirectoryGuard.EnsureScope(route, model.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand("""
                SELECT capability_resource, capability_feature, capability_action, display_name
                FROM identity_access.application_capabilities
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND model_version = @model_version
                ORDER BY capability_resource, capability_feature, capability_action;
                """, connection);
            AddModel(command, model);
            var result = new List<ApplicationCapability>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                result.Add(new ApplicationCapability(model,
                    new CapabilityKey(reader.GetString(0), reader.GetString(1), reader.GetString(2)), reader.GetString(3)));
            }
            return result.AsReadOnly();
        }

        private static void AddModel(NpgsqlCommand command, ApplicationSecurityModelReference model)
        {
            command.Parameters.AddWithValue("scope", model.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", model.Application.Value);
            command.Parameters.AddWithValue("model_version", model.Version);
        }

        private static void AddCapability(NpgsqlCommand command, CapabilityKey capability)
        {
            command.Parameters.AddWithValue("capability_resource", capability.Resource);
            command.Parameters.AddWithValue("capability_feature", capability.Feature);
            command.Parameters.AddWithValue("capability_action", capability.Action);
        }
    }
}
