using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    /// <summary>PostgreSQL-backed OrganisationProfile lifecycle persistence.</summary>
    public sealed class PostgreSqlOrganisationProfileStore(
        NpgsqlDataSource dataSource) : IOrganisationProfileStore
    {
        private const string Projection = """
            organisation_profile_id,
            identity_scope_id,
            tenant_id,
            organization_id,
            template_key,
            template_version,
            status,
            row_version,
            created_at,
            updated_at
            """;

        /// <inheritdoc />
        public async Task<Domain.OrganisationProfile?> GetAsync(
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken)
        {
            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profiles
                WHERE organisation_profile_id = @organisation_profile_id;
                """);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            return await reader.ReadAsync(cancellationToken)
                ? ReadProfile(reader)
                : null;
        }

        /// <inheritdoc />
        public async Task<Domain.OrganisationProfile?> FindByOrganizationAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(organization);

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profiles
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id;
                """);

            AddOrganizationParameters(command, organization);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            return await reader.ReadAsync(cancellationToken)
                ? ReadProfile(reader)
                : null;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Domain.OrganisationProfile>> ListAsync(
            Guid identityScopeId,
            Guid tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException(
                    "IdentityScopeId cannot be empty.",
                    nameof(identityScopeId));

            if (tenantId == Guid.Empty)
                throw new ArgumentException(
                    "TenantId cannot be empty.",
                    nameof(tenantId));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (limit is < 1 or > 500)
                throw new ArgumentOutOfRangeException(nameof(limit));

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profiles
                WHERE identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                ORDER BY organisation_profile_id
                OFFSET @offset
                LIMIT @limit;
                """);

            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                identityScopeId);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                tenantId);
            command.Parameters.AddWithValue(
                "offset",
                NpgsqlDbType.Integer,
                offset);
            command.Parameters.AddWithValue(
                "limit",
                NpgsqlDbType.Integer,
                limit);

            var profiles = new List<Domain.OrganisationProfile>();

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                profiles.Add(ReadProfile(reader));
            }

            return profiles;
        }

        /// <inheritdoc />
        public async Task<Domain.OrganisationProfile> CreateAsync(
            Domain.OrganisationProfile profile,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (profile.RowVersion != 0)
            {
                throw new ArgumentException(
                    "New OrganisationProfiles must use row version zero before persistence.",
                    nameof(profile));
            }

            await using var command = dataSource.CreateCommand($"""
                INSERT INTO organisation_profile.organisation_profiles
                (
                    organisation_profile_id,
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    template_key,
                    template_version,
                    status,
                    row_version,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @organisation_profile_id,
                    @identity_scope_id,
                    @tenant_id,
                    @organization_id,
                    @template_key,
                    @template_version,
                    @status,
                    1,
                    @created_at,
                    @updated_at
                )
                RETURNING {Projection};
                """);

            AddProfileParameters(
                command,
                profile,
                includeRowVersion: false);

            try
            {
                await using var reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "PostgreSQL did not return the created OrganisationProfile.");
                }

                return ReadProfile(reader);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.UniqueViolation &&
                string.Equals(
                    exception.ConstraintName,
                    "pk_organisation_profiles",
                    StringComparison.Ordinal))
            {
                throw new OrganisationProfileIdentityConflictException(
                    "OrganisationProfileId already exists.",
                    exception);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.UniqueViolation &&
                string.Equals(
                    exception.ConstraintName,
                    "uq_organisation_profiles_organization",
                    StringComparison.Ordinal))
            {
                throw new OrganisationProfileAlreadyExistsException(
                    "The Organization already has an OrganisationProfile.",
                    exception);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    exception.ConstraintName,
                    "fk_organisation_profiles_organization",
                    StringComparison.Ordinal))
            {
                throw new OrganisationProfileOrganizationNotFoundException(
                    "The referenced Organization does not exist.",
                    exception);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    exception.ConstraintName,
                    "fk_organisation_profiles_template_version",
                    StringComparison.Ordinal))
            {
                throw new OrganisationProfileTemplateVersionNotFoundException(
                    "The referenced template version does not exist.",
                    exception);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.CheckViolation &&
                string.Equals(
                    exception.ConstraintName,
                    "ck_organisation_profiles_template_published",
                    StringComparison.Ordinal))
            {
                throw new OrganisationProfileTemplateVersionNotPublishedException(
                    "New profile pins require a Published template version.",
                    exception);
            }
        }

        /// <inheritdoc />
        public async Task<Domain.OrganisationProfile?> UpdateAsync(
            Domain.OrganisationProfile profile,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (profile.RowVersion <= 0)
            {
                throw new ArgumentException(
                    "Persisted OrganisationProfiles require a positive row version.",
                    nameof(profile));
            }

            await using var command = dataSource.CreateCommand($"""
                UPDATE organisation_profile.organisation_profiles
                SET template_key = @template_key,
                    template_version = @template_version,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = @updated_at
                WHERE organisation_profile_id = @organisation_profile_id
                  AND identity_scope_id = @identity_scope_id
                  AND tenant_id = @tenant_id
                  AND organization_id = @organization_id
                  AND row_version = @row_version
                RETURNING {Projection};
                """);

            AddProfileParameters(
                command,
                profile,
                includeRowVersion: true);

            try
            {
                await using var reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    return ReadProfile(reader);
                }
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
                string.Equals(
                    exception.ConstraintName,
                    "fk_organisation_profiles_template_version",
                    StringComparison.Ordinal))
            {
                throw new OrganisationProfileTemplateVersionNotFoundException(
                    "The referenced template version does not exist.",
                    exception);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.CheckViolation &&
                string.Equals(
                    exception.ConstraintName,
                    "ck_organisation_profiles_template_published",
                    StringComparison.Ordinal))
            {
                throw new OrganisationProfileTemplateVersionNotPublishedException(
                    "New profile pins require a Published template version.",
                    exception);
            }

            var current = await GetAsync(
                    profile.OrganisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return null;
            }

            if (current.Organization != profile.Organization)
            {
                throw new OrganisationProfileIdentityConflictException(
                    "OrganisationProfile Organization identity is immutable.");
            }

            throw new OrganisationProfileConcurrencyException(
                $"OrganisationProfile expected row version {profile.RowVersion} " +
                $"but durable row version is {current.RowVersion}.");
        }

        private static Domain.OrganisationProfile ReadProfile(
            NpgsqlDataReader reader)
        {
            var templatePin =
                reader.IsDBNull(4)
                    ? null
                    : new OrganisationProfileTemplatePin(
                        new OrganisationProfileTemplateKey(
                            reader.GetString(4)),
                        new OrganisationProfileTemplateVersionNumber(
                            reader.GetInt32(5)));

            return new Domain.OrganisationProfile(
                new OrganisationProfileId(reader.GetGuid(0)),
                new OrganizationReference(
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetGuid(3)),
                templatePin,
                (OrganisationProfileStatus)reader.GetInt16(6),
                reader.GetInt64(7),
                reader.GetFieldValue<DateTimeOffset>(8),
                reader.GetFieldValue<DateTimeOffset>(9));
        }

        private static void AddProfileParameters(
            NpgsqlCommand command,
            Domain.OrganisationProfile profile,
            bool includeRowVersion)
        {
            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                profile.OrganisationProfileId.Value);

            AddOrganizationParameters(
                command,
                profile.Organization);

            if (profile.TemplatePin is null)
            {
                command.Parameters.AddWithValue(
                    "template_key",
                    NpgsqlDbType.Varchar,
                    DBNull.Value);
                command.Parameters.AddWithValue(
                    "template_version",
                    NpgsqlDbType.Integer,
                    DBNull.Value);
            }
            else
            {
                command.Parameters.AddWithValue(
                    "template_key",
                    NpgsqlDbType.Varchar,
                    profile.TemplatePin.TemplateKey.Value);
                command.Parameters.AddWithValue(
                    "template_version",
                    NpgsqlDbType.Integer,
                    profile.TemplatePin.Version.Value);
            }

            command.Parameters.AddWithValue(
                "status",
                NpgsqlDbType.Smallint,
                (short)profile.Status);
            command.Parameters.AddWithValue(
                "created_at",
                NpgsqlDbType.TimestampTz,
                profile.CreatedAt);
            command.Parameters.AddWithValue(
                "updated_at",
                NpgsqlDbType.TimestampTz,
                profile.UpdatedAt);

            if (includeRowVersion)
            {
                command.Parameters.AddWithValue(
                    "row_version",
                    NpgsqlDbType.Bigint,
                    profile.RowVersion);
            }
        }

        private static void AddOrganizationParameters(
            NpgsqlCommand command,
            OrganizationReference organization)
        {
            command.Parameters.AddWithValue(
                "identity_scope_id",
                NpgsqlDbType.Uuid,
                organization.IdentityScopeId);
            command.Parameters.AddWithValue(
                "tenant_id",
                NpgsqlDbType.Uuid,
                organization.TenantId);
            command.Parameters.AddWithValue(
                "organization_id",
                NpgsqlDbType.Uuid,
                organization.OrganizationId);
        }
    }
}
