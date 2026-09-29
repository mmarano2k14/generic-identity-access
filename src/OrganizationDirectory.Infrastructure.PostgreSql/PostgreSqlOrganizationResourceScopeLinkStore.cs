using Npgsql;
using NpgsqlTypes;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Infrastructure.PostgreSql
{
    /// <summary>PostgreSQL-backed application-aware Organization-to-ResourceScope linkage persistence.</summary>
    public sealed class PostgreSqlOrganizationResourceScopeLinkStore(
        NpgsqlDataSource dataSource) : IOrganizationResourceScopeLinkStore
    {
        private const string Projection = """
            l.identity_scope_id,
            l.tenant_id,
            l.organization_id,
            l.application_key,
            l.resource_scope_id,
            rs.scope_model_version,
            rs.scope_type_key,
            l.status,
            l.row_version,
            l.created_at,
            l.updated_at
            """;

        /// <inheritdoc />
        public async Task<OrganizationResourceScopeLink?> GetAsync(
            OrganizationReference organization,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(application);

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organization_resource_scope_links l
                INNER JOIN identity_access.resource_scopes rs
                    ON rs.identity_scope_id = l.identity_scope_id
                   AND rs.tenant_id = l.tenant_id
                   AND rs.application_key = l.application_key
                   AND rs.resource_scope_id = l.resource_scope_id
                WHERE l.identity_scope_id = @identity_scope_id
                  AND l.tenant_id = @tenant_id
                  AND l.organization_id = @organization_id
                  AND l.application_key = @application_key;
                """);

            AddOrganizationParameters(command, organization);
            command.Parameters.AddWithValue(
                "application_key",
                NpgsqlDbType.Varchar,
                application.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? ReadLink(reader)
                : null;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganizationResourceScopeLink>> ListAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organization_resource_scope_links l
                INNER JOIN identity_access.resource_scopes rs
                    ON rs.identity_scope_id = l.identity_scope_id
                   AND rs.tenant_id = l.tenant_id
                   AND rs.application_key = l.application_key
                   AND rs.resource_scope_id = l.resource_scope_id
                WHERE l.identity_scope_id = @identity_scope_id
                  AND l.tenant_id = @tenant_id
                  AND l.organization_id = @organization_id
                ORDER BY l.application_key;
                """);

            AddOrganizationParameters(command, organization);

            var links = new List<OrganizationResourceScopeLink>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                links.Add(ReadLink(reader));
            }

            return links;
        }

        /// <inheritdoc />
        public async Task<OrganizationResourceScopeLink> CreateAsync(
            OrganizationResourceScopeLink link,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(link);

            if (link.RowVersion != 0)
            {
                throw new ArgumentException(
                    "New ResourceScope links must use row version zero before persistence.",
                    nameof(link));
            }

            await using var command = dataSource.CreateCommand($"""
                INSERT INTO organization_directory.organization_resource_scope_links
                (
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    application_key,
                    resource_scope_id,
                    status,
                    row_version,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @identity_scope_id,
                    @tenant_id,
                    @organization_id,
                    @application_key,
                    @resource_scope_id,
                    @status,
                    1,
                    @created_at,
                    @updated_at
                )
                RETURNING
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    application_key,
                    resource_scope_id,
                    @scope_model_version AS scope_model_version,
                    @scope_type_key AS scope_type_key,
                    status,
                    row_version,
                    created_at,
                    updated_at;
                """);

            AddLinkParameters(command, link, includeRowVersion: false);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "PostgreSQL did not return the created ResourceScope link.");
                }

                return ReadLink(reader);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.UniqueViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "pk_organization_resource_scope_links",
                    StringComparison.Ordinal))
            {
                throw new OrganizationResourceScopeLinkAlreadyExistsException(
                    "This Organization already has a ResourceScope link for the application.",
                    ex);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.UniqueViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "uq_organization_resource_scope_links_scope",
                    StringComparison.Ordinal))
            {
                throw new OrganizationResourceScopeAlreadyLinkedException(
                    "This ResourceScope is already linked to another Organization.",
                    ex);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "fk_organization_resource_scope_links_organization",
                    StringComparison.Ordinal))
            {
                throw new OrganizationResourceScopeOrganizationNotFoundException(
                    "The Organization referenced by the ResourceScope link does not exist.",
                    ex);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "fk_organization_resource_scope_links_resource_scope",
                    StringComparison.Ordinal))
            {
                throw new OrganizationResourceScopeReferenceNotFoundException(
                    "The Identity Access ResourceScope referenced by the link does not exist in this boundary.",
                    ex);
            }
        }

        /// <inheritdoc />
        public async Task<OrganizationResourceScopeLink?> UpdateAsync(
            OrganizationResourceScopeLink link,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(link);

            if (link.RowVersion <= 0)
            {
                throw new ArgumentException(
                    "Persisted ResourceScope links require a positive row version.",
                    nameof(link));
            }

            await using var command = dataSource.CreateCommand($"""
                UPDATE organization_directory.organization_resource_scope_links
                SET resource_scope_id = @resource_scope_id,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = @updated_at
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND application_key = @application_key
                  AND row_version = @row_version
                RETURNING
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    application_key,
                    resource_scope_id,
                    @scope_model_version AS scope_model_version,
                    @scope_type_key AS scope_type_key,
                    status,
                    row_version,
                    created_at,
                    updated_at;
                """);

            AddLinkParameters(command, link, includeRowVersion: true);

            try
            {
                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return ReadLink(reader);
                    }
                }
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.UniqueViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "uq_organization_resource_scope_links_scope",
                    StringComparison.Ordinal))
            {
                throw new OrganizationResourceScopeAlreadyLinkedException(
                    "This ResourceScope is already linked to another Organization.",
                    ex);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "fk_organization_resource_scope_links_resource_scope",
                    StringComparison.Ordinal))
            {
                throw new OrganizationResourceScopeReferenceNotFoundException(
                    "The Identity Access ResourceScope referenced by the link does not exist in this boundary.",
                    ex);
            }

            var currentVersion = await GetCurrentRowVersionAsync(
                    link.Organization,
                    link.ResourceScope.ApplicationKey,
                    cancellationToken)
                .ConfigureAwait(false);

            if (currentVersion is null)
            {
                return null;
            }

            throw new OrganizationResourceScopeLinkConcurrencyException(
                $"ResourceScope link expected row version {link.RowVersion} " +
                $"but durable row version is {currentVersion.Value}.");
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(
            OrganizationReference organization,
            ApplicationKey application,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(application);
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            await using var command = dataSource.CreateCommand("""
                DELETE FROM organization_directory.organization_resource_scope_links
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND application_key = @application_key
                  AND row_version = @row_version;
                """);

            AddOrganizationParameters(command, organization);
            command.Parameters.AddWithValue(
                "application_key",
                NpgsqlDbType.Varchar,
                application.Value);
            command.Parameters.AddWithValue(
                "row_version",
                NpgsqlDbType.Bigint,
                expectedRowVersion);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 1)
            {
                return true;
            }

            var currentVersion = await GetCurrentRowVersionAsync(
                    organization,
                    application,
                    cancellationToken)
                .ConfigureAwait(false);

            if (currentVersion is null)
            {
                return false;
            }

            throw new OrganizationResourceScopeLinkConcurrencyException(
                $"ResourceScope link expected row version {expectedRowVersion} " +
                $"but durable row version is {currentVersion.Value}.");
        }

        private async Task<long?> GetCurrentRowVersionAsync(
            OrganizationReference organization,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            await using var command = dataSource.CreateCommand("""
                SELECT row_version
                FROM organization_directory.organization_resource_scope_links
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND application_key = @application_key;
                """);

            AddOrganizationParameters(command, organization);
            command.Parameters.AddWithValue(
                "application_key",
                NpgsqlDbType.Varchar,
                application.Value);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is null or DBNull
                ? null
                : Convert.ToInt64(
                    result,
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        private static OrganizationResourceScopeLink ReadLink(
            NpgsqlDataReader reader)
        {
            var identityScopeId = new IdentityScopeId(reader.GetGuid(0));
            var tenantId = new TenantId(reader.GetGuid(1));
            var organization = new OrganizationReference(
                identityScopeId,
                tenantId,
                new OrganizationId(reader.GetGuid(2)));

            var application = new ApplicationKey(reader.GetString(3));

            var resourceScope = new ResourceScopeReference(
                identityScopeId,
                tenantId,
                application,
                new ResourceScopeId(reader.GetGuid(4)),
                new ScopeType(reader.GetString(6)),
                new SecurityModelVersion(reader.GetInt32(5)));

            return new OrganizationResourceScopeLink(
                organization,
                resourceScope,
                (OrganizationResourceScopeLinkStatus)reader.GetInt16(7),
                reader.GetInt64(8),
                reader.GetFieldValue<DateTimeOffset>(9),
                reader.GetFieldValue<DateTimeOffset>(10));
        }

        private static void AddLinkParameters(
            NpgsqlCommand command,
            OrganizationResourceScopeLink link,
            bool includeRowVersion)
        {
            AddOrganizationParameters(command, link.Organization);

            command.Parameters.AddWithValue(
                "application_key",
                NpgsqlDbType.Varchar,
                link.ResourceScope.ApplicationKey.Value);
            command.Parameters.AddWithValue(
                "resource_scope_id",
                NpgsqlDbType.Uuid,
                link.ResourceScope.ResourceScopeId.Value);
            command.Parameters.AddWithValue(
                "scope_model_version",
                NpgsqlDbType.Integer,
                link.ResourceScope.ModelVersion.Value);
            command.Parameters.AddWithValue(
                "scope_type_key",
                NpgsqlDbType.Varchar,
                link.ResourceScope.ScopeType.Value);
            command.Parameters.AddWithValue(
                "status",
                NpgsqlDbType.Smallint,
                (short)link.Status);
            command.Parameters.AddWithValue(
                "created_at",
                NpgsqlDbType.TimestampTz,
                link.CreatedAt);
            command.Parameters.AddWithValue(
                "updated_at",
                NpgsqlDbType.TimestampTz,
                link.UpdatedAt);

            if (includeRowVersion)
            {
                command.Parameters.AddWithValue(
                    "row_version",
                    NpgsqlDbType.Bigint,
                    link.RowVersion);
            }
        }

        private static void AddOrganizationParameters(
            NpgsqlCommand command,
            OrganizationReference organization)
        {
            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                organization.IdentityScopeId.Value);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                organization.TenantId.Value);
            command.Parameters.AddWithValue(
                "organization_id",
                NpgsqlDbType.Uuid,
                organization.OrganizationId.Value);
        }
    }
}
