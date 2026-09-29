using Npgsql;
using NpgsqlTypes;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Infrastructure.PostgreSql
{
    /// <summary>PostgreSQL-backed explicit organization-membership persistence.</summary>
    public sealed class PostgreSqlOrganizationMembershipStore(
        NpgsqlDataSource dataSource) : IOrganizationMembershipStore
    {
        private const string Projection = """
            identity_scope_id,
            tenant_id,
            organization_id,
            tenant_membership_id,
            status,
            row_version,
            created_at,
            updated_at
            """;

        /// <inheritdoc />
        public async Task<OrganizationMembership?> GetAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken)
        {
            ValidateBoundary(organization, tenantMembership);

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organization_memberships
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND tenant_membership_id = @tenant_membership_id;
                """);

            AddKeyParameters(command, organization, tenantMembership);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? ReadMembership(reader)
                : null;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganizationMembership>> ListForOrganizationAsync(
            OrganizationReference organization,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ValidatePaging(offset, limit);

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organization_memberships
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                ORDER BY tenant_membership_id
                OFFSET @offset
                LIMIT @limit;
                """);

            AddOrganizationParameters(command, organization);
            command.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, offset);
            command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);

            return await ReadManyAsync(command, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganizationMembership>> ListForTenantMembershipAsync(
            TenantMembershipReference tenantMembership,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tenantMembership);
            ValidatePaging(offset, limit);

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organization_directory.organization_memberships
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND tenant_membership_id = @tenant_membership_id
                ORDER BY organization_id
                OFFSET @offset
                LIMIT @limit;
                """);

            AddTenantMembershipParameters(command, tenantMembership);
            command.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, offset);
            command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);

            return await ReadManyAsync(command, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<OrganizationMembership> CreateAsync(
            OrganizationMembership membership,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membership);

            if (membership.RowVersion != 0)
            {
                throw new ArgumentException(
                    "New organization memberships must use row version zero before persistence.",
                    nameof(membership));
            }

            await using var command = dataSource.CreateCommand($"""
                INSERT INTO organization_directory.organization_memberships
                (
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    tenant_membership_id,
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
                    @tenant_membership_id,
                    @status,
                    1,
                    @created_at,
                    @updated_at
                )
                RETURNING {Projection};
                """);

            AddMembershipParameters(command, membership, includeRowVersion: false);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "PostgreSQL did not return the created organization membership.");
                }

                return ReadMembership(reader);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new OrganizationMembershipAlreadyExistsException(
                    "The tenant membership already belongs to this organization.",
                    ex);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "fk_organization_memberships_tenant_membership",
                    StringComparison.Ordinal))
            {
                throw new TenantMembershipReferenceNotFoundException(
                    "The referenced Identity Access tenant membership does not exist in this tenant.",
                    ex);
            }
            catch (PostgresException ex) when (
                ex.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    ex.ConstraintName,
                    "fk_organization_memberships_organization",
                    StringComparison.Ordinal))
            {
                throw new OrganizationMembershipReferenceNotFoundException(
                    "The referenced organization does not exist in this tenant.",
                    ex);
            }
        }

        /// <inheritdoc />
        public async Task<OrganizationMembership?> UpdateAsync(
            OrganizationMembership membership,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membership);

            if (membership.RowVersion <= 0)
            {
                throw new ArgumentException(
                    "Persisted organization memberships require a positive row version.",
                    nameof(membership));
            }

            await using var command = dataSource.CreateCommand($"""
                UPDATE organization_directory.organization_memberships
                SET status = @status,
                    row_version = row_version + 1,
                    updated_at = @updated_at
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND tenant_membership_id = @tenant_membership_id
                  AND row_version = @row_version
                RETURNING {Projection};
                """);

            AddMembershipParameters(command, membership, includeRowVersion: true);

            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    return ReadMembership(reader);
                }
            }

            var currentVersion = await GetCurrentRowVersionAsync(
                    membership.Organization,
                    membership.TenantMembership,
                    cancellationToken)
                .ConfigureAwait(false);

            if (currentVersion is null)
            {
                return null;
            }

            throw new OrganizationMembershipConcurrencyException(
                $"Organization membership expected row version {membership.RowVersion} " +
                $"but durable row version is {currentVersion.Value}.");
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            ValidateBoundary(organization, tenantMembership);
            if (expectedRowVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRowVersion));

            await using var command = dataSource.CreateCommand("""
                DELETE FROM organization_directory.organization_memberships
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND tenant_membership_id = @tenant_membership_id
                  AND row_version = @row_version;
                """);

            AddKeyParameters(command, organization, tenantMembership);
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
                    tenantMembership,
                    cancellationToken)
                .ConfigureAwait(false);

            if (currentVersion is null)
            {
                return false;
            }

            throw new OrganizationMembershipConcurrencyException(
                $"Organization membership expected row version {expectedRowVersion} " +
                $"but durable row version is {currentVersion.Value}.");
        }

        private async Task<long?> GetCurrentRowVersionAsync(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership,
            CancellationToken cancellationToken)
        {
            await using var command = dataSource.CreateCommand("""
                SELECT row_version
                FROM organization_directory.organization_memberships
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND tenant_membership_id = @tenant_membership_id;
                """);

            AddKeyParameters(command, organization, tenantMembership);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is null or DBNull
                ? null
                : Convert.ToInt64(
                    result,
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        private static async Task<IReadOnlyList<OrganizationMembership>> ReadManyAsync(
            NpgsqlCommand command,
            CancellationToken cancellationToken)
        {
            var memberships = new List<OrganizationMembership>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                memberships.Add(ReadMembership(reader));
            }

            return memberships;
        }

        private static OrganizationMembership ReadMembership(NpgsqlDataReader reader)
        {
            var identityScopeId = new IdentityScopeId(reader.GetGuid(0));
            var tenantId = new TenantId(reader.GetGuid(1));
            var organization = new OrganizationReference(
                identityScopeId,
                tenantId,
                new OrganizationId(reader.GetGuid(2)));
            var tenantMembership = new TenantMembershipReference(
                identityScopeId,
                tenantId,
                new TenantMembershipId(reader.GetGuid(3)));

            return new OrganizationMembership(
                organization,
                tenantMembership,
                (OrganizationMembershipStatus)reader.GetInt16(4),
                reader.GetInt64(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetFieldValue<DateTimeOffset>(7));
        }

        private static void AddMembershipParameters(
            NpgsqlCommand command,
            OrganizationMembership membership,
            bool includeRowVersion)
        {
            AddKeyParameters(
                command,
                membership.Organization,
                membership.TenantMembership);

            command.Parameters.AddWithValue(
                "status",
                NpgsqlDbType.Smallint,
                (short)membership.Status);
            command.Parameters.AddWithValue(
                "created_at",
                NpgsqlDbType.TimestampTz,
                membership.CreatedAt);
            command.Parameters.AddWithValue(
                "updated_at",
                NpgsqlDbType.TimestampTz,
                membership.UpdatedAt);

            if (includeRowVersion)
            {
                command.Parameters.AddWithValue(
                    "row_version",
                    NpgsqlDbType.Bigint,
                    membership.RowVersion);
            }
        }

        private static void AddKeyParameters(
            NpgsqlCommand command,
            OrganizationReference organization,
            TenantMembershipReference tenantMembership)
        {
            ValidateBoundary(organization, tenantMembership);
            AddOrganizationParameters(command, organization);
            command.Parameters.AddWithValue(
                "tenant_membership_id",
                NpgsqlDbType.Uuid,
                tenantMembership.TenantMembershipId.Value);
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

        private static void AddTenantMembershipParameters(
            NpgsqlCommand command,
            TenantMembershipReference tenantMembership)
        {
            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                tenantMembership.IdentityScopeId.Value);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                tenantMembership.TenantId.Value);
            command.Parameters.AddWithValue(
                "tenant_membership_id",
                NpgsqlDbType.Uuid,
                tenantMembership.TenantMembershipId.Value);
        }

        private static void ValidateBoundary(
            OrganizationReference organization,
            TenantMembershipReference tenantMembership)
        {
            ArgumentNullException.ThrowIfNull(organization);
            ArgumentNullException.ThrowIfNull(tenantMembership);

            if (organization.IdentityScopeId != tenantMembership.IdentityScopeId ||
                organization.TenantId != tenantMembership.TenantId)
            {
                throw new ArgumentException(
                    "Organization membership cannot cross identity-scope or tenant boundaries.",
                    nameof(tenantMembership));
            }
        }

        private static void ValidatePaging(int offset, int limit)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(limit));
        }
    }
}
