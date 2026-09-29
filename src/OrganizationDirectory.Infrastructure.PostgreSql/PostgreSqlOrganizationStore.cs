using Npgsql;
using NpgsqlTypes;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Infrastructure.PostgreSql
{
    /// <summary>PostgreSQL-backed organization persistence with tenant isolation and optimistic concurrency.</summary>
    public sealed class PostgreSqlOrganizationStore : IOrganizationStore
    {
        private const string Projection = """
            identity_scope_id,
            tenant_id,
            organization_id,
            organization_key,
            display_name,
            organization_type,
            parent_organization_id,
            status,
            row_version,
            created_at,
            updated_at
            """;

        private readonly NpgsqlDataSource _dataSource;

        /// <summary>Initializes the store over a bounded Npgsql data source.</summary>
        public PostgreSqlOrganizationStore(NpgsqlDataSource dataSource)
        {
            ArgumentNullException.ThrowIfNull(dataSource);
            _dataSource = dataSource;
        }

        /// <inheritdoc />
        public async Task<Organization?> GetAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);

            await using var command = _dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organizations
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id;
                """);

            AddReferenceParameters(command, reference);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadOrganization(reader) : null;
        }

        /// <inheritdoc />
        public async Task<Organization?> FindByKeyAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            OrganizationKey key,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(key);

            await using var command = _dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organizations
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_key = @organization_key;
                """);

            AddScopeParameters(command, identityScopeId, tenantId);
            command.Parameters.AddWithValue("organization_key", NpgsqlDbType.Varchar, key.Value);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadOrganization(reader) : null;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Organization>> ListAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(limit));

            await using var command = _dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organizations
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                ORDER BY organization_key, organization_id
                OFFSET @offset
                LIMIT @limit;
                """);

            AddScopeParameters(command, identityScopeId, tenantId);
            command.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, offset);
            command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);

            var organizations = new List<Organization>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                organizations.Add(ReadOrganization(reader));
            }

            return organizations;
        }

        /// <inheritdoc />
        public async Task<Organization> CreateAsync(
            Organization organization,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            if (organization.RowVersion != 0)
                throw new ArgumentException("New organizations must use row version zero before persistence.", nameof(organization));

            await using var command = _dataSource.CreateCommand($"""
                INSERT INTO organization_directory.organizations
                (
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    organization_key,
                    display_name,
                    organization_type,
                    parent_organization_id,
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
                    @organization_key,
                    @display_name,
                    @organization_type,
                    @parent_organization_id,
                    @status,
                    1,
                    @created_at,
                    @updated_at
                )
                RETURNING {Projection};
                """);

            AddOrganizationParameters(command, organization, includeRowVersion: false);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException("PostgreSQL did not return the created organization.");
                return ReadOrganization(reader);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(ex.ConstraintName, "fk_organizations_tenant", StringComparison.Ordinal))
            {
                throw new OrganizationTenantNotFoundException(
                    $"Tenant '{organization.Reference.TenantId}' does not exist in Identity Access.",
                    ex);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw TranslateUniqueViolation(organization, ex);
            }
            catch (PostgresException ex) when (IsHierarchyViolation(ex))
            {
                throw new OrganizationHierarchyConflictException(ex.MessageText, ex);
            }
        }

        /// <inheritdoc />
        public async Task<Organization?> UpdateAsync(
            Organization organization,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            if (organization.RowVersion <= 0)
                throw new ArgumentException("Persisted organizations require a positive row version.", nameof(organization));

            await using var command = _dataSource.CreateCommand($"""
                UPDATE organization_directory.organizations
                SET organization_key = @organization_key,
                    display_name = @display_name,
                    organization_type = @organization_type,
                    parent_organization_id = @parent_organization_id,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = @updated_at
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND row_version = @row_version
                RETURNING {Projection};
                """);

            AddOrganizationParameters(command, organization, includeRowVersion: true);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                    return ReadOrganization(reader);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(ex.ConstraintName, "fk_organizations_tenant", StringComparison.Ordinal))
            {
                throw new OrganizationTenantNotFoundException(
                    $"Tenant '{organization.Reference.TenantId}' does not exist in Identity Access.",
                    ex);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw TranslateUniqueViolation(organization, ex);
            }
            catch (PostgresException ex) when (IsHierarchyViolation(ex))
            {
                throw new OrganizationHierarchyConflictException(ex.MessageText, ex);
            }

            var currentVersion = await GetCurrentRowVersionAsync(organization.Reference, cancellationToken);
            if (currentVersion is null) return null;

            throw new OrganizationConcurrencyException(
                $"Organization '{organization.Reference.OrganizationId}' expected row version {organization.RowVersion} but durable row version is {currentVersion.Value}.");
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(
            OrganizationReference reference,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reference);
            if (expectedRowVersion <= 0) throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            await using var command = _dataSource.CreateCommand("""
                DELETE FROM organization_directory.organizations
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND row_version = @row_version;
                """);

            AddReferenceParameters(command, reference);
            command.Parameters.AddWithValue("row_version", NpgsqlDbType.Bigint, expectedRowVersion);

            try
            {
                var affected = await command.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 1) return true;
            }
            catch (PostgresException ex) when (IsDeleteHierarchyViolation(ex))
            {
                throw new OrganizationHierarchyConflictException(
                    "Organization cannot be deleted while child organizations still reference it.",
                    ex);
            }

            var currentVersion = await GetCurrentRowVersionAsync(reference, cancellationToken);
            if (currentVersion is null) return false;

            throw new OrganizationConcurrencyException(
                $"Organization '{reference.OrganizationId}' expected row version {expectedRowVersion} but durable row version is {currentVersion.Value}.");
        }

        private async Task<long?> GetCurrentRowVersionAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("""
                SELECT row_version
                FROM organization_directory.organizations
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id;
                """);

            AddReferenceParameters(command, reference);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null or DBNull ? null : Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
        }


        private static Exception TranslateUniqueViolation(Organization organization, PostgresException exception)
        {
            if (string.Equals(exception.ConstraintName, "uq_organizations_key", StringComparison.Ordinal))
            {
                return new OrganizationKeyConflictException(
                    $"Organization key '{organization.Key.Value}' already exists in the tenant.",
                    exception);
            }

            return new OrganizationIdentityConflictException(
                $"Organization identity '{organization.Reference.OrganizationId}' already exists in the tenant.",
                exception);
        }

        private static bool IsHierarchyViolation(PostgresException exception) =>
            exception.SqlState is PostgresErrorCodes.CheckViolation or PostgresErrorCodes.ForeignKeyViolation;

        private static bool IsDeleteHierarchyViolation(PostgresException exception) =>
            exception.SqlState is PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.RestrictViolation;

        private static void AddOrganizationParameters(
            NpgsqlCommand command,
            Organization organization,
            bool includeRowVersion)
        {
            AddReferenceParameters(command, organization.Reference);
            command.Parameters.AddWithValue("organization_key", NpgsqlDbType.Varchar, organization.Key.Value);
            command.Parameters.AddWithValue("display_name", NpgsqlDbType.Varchar, organization.DisplayName);
            command.Parameters.AddWithValue("organization_type", NpgsqlDbType.Varchar, organization.Type.Value);

            var parent = command.Parameters.Add("parent_organization_id", NpgsqlDbType.Uuid);
            parent.Value = organization.Parent is null
                ? DBNull.Value
                : organization.Parent.OrganizationId.Value;

            command.Parameters.AddWithValue("status", NpgsqlDbType.Smallint, (short)organization.Status);
            command.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, organization.CreatedAt.ToUniversalTime());
            command.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, organization.UpdatedAt.ToUniversalTime());

            if (includeRowVersion)
                command.Parameters.AddWithValue("row_version", NpgsqlDbType.Bigint, organization.RowVersion);
        }

        private static void AddReferenceParameters(
            NpgsqlCommand command,
            OrganizationReference reference)
        {
            AddScopeParameters(command, reference.IdentityScopeId, reference.TenantId);
            command.Parameters.AddWithValue("organization_id", NpgsqlDbType.Uuid, reference.OrganizationId.Value);
        }

        private static void AddScopeParameters(
            NpgsqlCommand command,
            IdentityScopeId identityScopeId,
            TenantId tenantId)
        {
            command.Parameters.AddWithValue("identity_scope_id", NpgsqlDbType.Uuid, identityScopeId.Value);
            command.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId.Value);
        }

        private static Organization ReadOrganization(NpgsqlDataReader reader)
        {
            var identityScopeId = new IdentityScopeId(reader.GetGuid(0));
            var tenantId = new TenantId(reader.GetGuid(1));
            var organizationId = new OrganizationId(reader.GetGuid(2));
            var reference = new OrganizationReference(identityScopeId, tenantId, organizationId);

            OrganizationReference? parent = null;
            if (!reader.IsDBNull(6))
            {
                parent = new OrganizationReference(
                    identityScopeId,
                    tenantId,
                    new OrganizationId(reader.GetGuid(6)));
            }

            return new Organization(
                reference,
                new OrganizationKey(reader.GetString(3)),
                reader.GetString(4),
                new OrganizationType(reader.GetString(5)),
                parent,
                (OrganizationStatus)reader.GetInt16(7),
                reader.GetInt64(8),
                ToDateTimeOffset(reader.GetFieldValue<DateTime>(9)),
                ToDateTimeOffset(reader.GetFieldValue<DateTime>(10)));
        }

        private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
            new(value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime());
    }
}
