using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Npgsql;

namespace IdentityAccess.Infrastructure.PostgreSql.Directory
{
    /// <summary>Atomically persists and reads manifest-backed application security catalogs.</summary>
    internal sealed class PostgreSqlApplicationSecurityCatalogStore(
        IIdentityDatabaseConnectionFactory connectionFactory) : IApplicationSecurityCatalogStore
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<RegisteredApplicationSecurityModel>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            PostgreSqlDirectoryGuard.EnsureScope(route, identityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            return await LoadAsync(connection, identityScopeId, application, null, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<RegisteredApplicationSecurityModel?> GetAsync(
            ResolvedDatabaseRoute route,
            ApplicationSecurityModelReference model,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(model);
            PostgreSqlDirectoryGuard.EnsureScope(route, model.IdentityScopeId);
            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);
            var models = await LoadAsync(
                    connection,
                    model.IdentityScopeId,
                    model.Application,
                    model.Version,
                    cancellationToken)
                .ConfigureAwait(false);
            return models.Count == 0 ? null : models[0];
        }

        /// <inheritdoc />
        public async Task<RegisteredApplicationSecurityModel> RegisterAsync(
            ResolvedDatabaseRoute route,
            RegisteredApplicationSecurityModel model,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(model);
            PostgreSqlDirectoryGuard.EnsureScope(route, model.Reference.IdentityScopeId);

            await using var connection = (NpgsqlConnection)await connectionFactory.OpenAsync(route, cancellationToken)
                .ConfigureAwait(false);

            var existing = await LoadAsync(
                    connection,
                    model.Reference.IdentityScopeId,
                    model.Reference.Application,
                    model.Reference.Version,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existing.Count > 0)
            {
                return SameManifest(existing[0], model) ? existing[0] : throw new IdentityConcurrencyException();
            }

            if (await ModelRowExistsAsync(connection, model.Reference, cancellationToken).ConfigureAwait(false))
            {
                throw new IdentityConcurrencyException();
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var inserted = await InsertModelAsync(connection, transaction, model.Reference, cancellationToken)
                .ConfigureAwait(false);
            if (!inserted)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                var concurrent = await LoadAsync(
                        connection,
                        model.Reference.IdentityScopeId,
                        model.Reference.Application,
                        model.Reference.Version,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (concurrent.Count > 0 && SameManifest(concurrent[0], model))
                {
                    return concurrent[0];
                }

                throw new IdentityConcurrencyException();
            }

            await InsertRegistrationAsync(connection, transaction, model, cancellationToken).ConfigureAwait(false);
            await InsertNamespacesAsync(connection, transaction, model, cancellationToken).ConfigureAwait(false);
            await InsertCapabilitiesAsync(connection, transaction, model, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return model;
        }

        private static async Task<IReadOnlyList<RegisteredApplicationSecurityModel>> LoadAsync(
            NpgsqlConnection connection,
            Guid identityScopeId,
            ApplicationKey application,
            int? modelVersion,
            CancellationToken cancellationToken)
        {
            var registrations = new Dictionary<int, ApplicationSecurityCatalogRegistrationRow>();
            await using (var command = new NpgsqlCommand($"""
                SELECT model_version, manifest_schema_version, rbac_project, manifest_sha256
                FROM identity_access.application_security_model_registrations
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  {(modelVersion is null ? string.Empty : "AND model_version = @model_version")}
                ORDER BY model_version DESC;
                """, connection))
            {
                command.Parameters.AddWithValue("scope", identityScopeId);
                command.Parameters.AddWithValue("application_key", application.Value);
                if (modelVersion is not null) command.Parameters.AddWithValue("model_version", modelVersion.Value);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    registrations.Add(
                        reader.GetInt32(0),
                        new ApplicationSecurityCatalogRegistrationRow(reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
                }
            }

            if (registrations.Count == 0) return Array.Empty<RegisteredApplicationSecurityModel>();

            var namespaces = registrations.Keys.ToDictionary(version => version, _ => new List<string>());
            await using (var command = new NpgsqlCommand($"""
                SELECT model_version, rbac_namespace
                FROM identity_access.application_security_namespaces
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  {(modelVersion is null ? string.Empty : "AND model_version = @model_version")}
                ORDER BY model_version DESC, rbac_namespace;
                """, connection))
            {
                command.Parameters.AddWithValue("scope", identityScopeId);
                command.Parameters.AddWithValue("application_key", application.Value);
                if (modelVersion is not null) command.Parameters.AddWithValue("model_version", modelVersion.Value);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (namespaces.TryGetValue(reader.GetInt32(0), out var values)) values.Add(reader.GetString(1));
                }
            }

            var capabilities = registrations.Keys.ToDictionary(
                version => version,
                _ => new List<ApplicationSecurityManifestCapability>());
            await using (var command = new NpgsqlCommand($"""
                SELECT model_version, capability_resource, capability_feature, capability_action, display_name
                FROM identity_access.application_capabilities
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  {(modelVersion is null ? string.Empty : "AND model_version = @model_version")}
                ORDER BY model_version DESC, capability_resource, capability_feature, capability_action;
                """, connection))
            {
                command.Parameters.AddWithValue("scope", identityScopeId);
                command.Parameters.AddWithValue("application_key", application.Value);
                if (modelVersion is not null) command.Parameters.AddWithValue("model_version", modelVersion.Value);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!capabilities.TryGetValue(reader.GetInt32(0), out var values)) continue;
                    values.Add(new ApplicationSecurityManifestCapability(
                        new CapabilityKey(reader.GetString(1), reader.GetString(2), reader.GetString(3)),
                        reader.GetString(4)));
                }
            }

            return registrations
                .OrderByDescending(entry => entry.Key)
                .Select(entry =>
                {
                    var manifest = new ApplicationSecurityManifest(
                        entry.Value.SchemaVersion,
                        application,
                        entry.Key,
                        entry.Value.RbacProject,
                        namespaces[entry.Key],
                        capabilities[entry.Key]);
                    return new RegisteredApplicationSecurityModel(identityScopeId, manifest, entry.Value.ManifestSha256);
                })
                .ToArray();
        }

        private static async Task<bool> ModelRowExistsAsync(
            NpgsqlConnection connection,
            ApplicationSecurityModelReference model,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT 1
                FROM identity_access.application_security_models
                WHERE identity_scope_id = @scope
                  AND application_key = @application_key
                  AND model_version = @model_version;
                """, connection);
            AddModel(command, model);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
        }

        private static async Task<bool> InsertModelAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            ApplicationSecurityModelReference model,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.application_security_models
                    (identity_scope_id, application_key, model_version)
                VALUES (@scope, @application_key, @model_version)
                ON CONFLICT DO NOTHING
                RETURNING 1;
                """, connection, transaction);
            AddModel(command, model);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
        }

        private static async Task InsertRegistrationAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            RegisteredApplicationSecurityModel model,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO identity_access.application_security_model_registrations
                    (identity_scope_id, application_key, model_version,
                     manifest_schema_version, rbac_project, manifest_sha256)
                VALUES (@scope, @application_key, @model_version,
                        @schema_version, @rbac_project, @manifest_sha256);
                """, connection, transaction);
            AddModel(command, model.Reference);
            command.Parameters.AddWithValue("schema_version", model.Manifest.SchemaVersion);
            command.Parameters.AddWithValue("rbac_project", model.Manifest.RbacProject);
            command.Parameters.AddWithValue("manifest_sha256", model.ManifestSha256);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task InsertNamespacesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            RegisteredApplicationSecurityModel model,
            CancellationToken cancellationToken)
        {
            foreach (var namespaceValue in model.Manifest.RbacNamespaces)
            {
                await using var command = new NpgsqlCommand("""
                    INSERT INTO identity_access.application_security_namespaces
                        (identity_scope_id, application_key, model_version, rbac_namespace)
                    VALUES (@scope, @application_key, @model_version, @rbac_namespace);
                    """, connection, transaction);
                AddModel(command, model.Reference);
                command.Parameters.AddWithValue("rbac_namespace", namespaceValue);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task InsertCapabilitiesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            RegisteredApplicationSecurityModel model,
            CancellationToken cancellationToken)
        {
            foreach (var capability in model.Capabilities)
            {
                await using var command = new NpgsqlCommand("""
                    INSERT INTO identity_access.application_capabilities
                        (identity_scope_id, application_key, model_version,
                         capability_resource, capability_feature, capability_action, display_name)
                    VALUES (@scope, @application_key, @model_version,
                            @resource, @feature, @action, @display_name);
                    """, connection, transaction);
                AddModel(command, model.Reference);
                command.Parameters.AddWithValue("resource", capability.Key.Resource);
                command.Parameters.AddWithValue("feature", capability.Key.Feature);
                command.Parameters.AddWithValue("action", capability.Key.Action);
                command.Parameters.AddWithValue("display_name", capability.DisplayName);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static void AddModel(NpgsqlCommand command, ApplicationSecurityModelReference model)
        {
            command.Parameters.AddWithValue("scope", model.IdentityScopeId);
            command.Parameters.AddWithValue("application_key", model.Application.Value);
            command.Parameters.AddWithValue("model_version", model.Version);
        }

        private static bool SameManifest(
            RegisteredApplicationSecurityModel left,
            RegisteredApplicationSecurityModel right) =>
            string.Equals(left.ManifestSha256, right.ManifestSha256, StringComparison.Ordinal);

    }
}
