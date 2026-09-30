using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed class PostgreSqlOrganisationProfileVersionReader(
        NpgsqlDataSource dataSource)
    {
        private const string Projection = """
            v.profile_version,
            v.template_key,
            v.template_version,
            v.content_hash,
            v.resolved_at,
            p.identity_scope_id,
            p.tenant_id,
            p.organization_id
            """;

        public async Task<EffectiveOrganisationProfile?> GetAsync(
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionNumber version,
            CancellationToken cancellationToken)
        {
            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await ReadOneAsync(
                    connection,
                    organisationProfileId,
                    version,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<EffectiveOrganisationProfile?> GetLatestAsync(
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken)
        {
            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using var command = new NpgsqlCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profile_versions v
                INNER JOIN organisation_profile.organisation_profiles p
                    ON p.organisation_profile_id =
                       v.organisation_profile_id
                WHERE v.organisation_profile_id =
                    @organisation_profile_id
                ORDER BY v.profile_version DESC
                LIMIT 1;
                """,
                connection);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            OrganisationProfileVersionRow? row = null;

            await using (var reader =
                await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    row = ReadRow(reader);
                }
            }

            return row is null
                ? null
                : await MaterializeAsync(
                        connection,
                        organisationProfileId,
                        row,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<EffectiveOrganisationProfile>> ListAsync(
            OrganisationProfileId organisationProfileId,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (limit is < 1 or > 500)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using var command = new NpgsqlCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profile_versions v
                INNER JOIN organisation_profile.organisation_profiles p
                    ON p.organisation_profile_id =
                       v.organisation_profile_id
                WHERE v.organisation_profile_id =
                    @organisation_profile_id
                ORDER BY v.profile_version
                OFFSET @offset
                LIMIT @limit;
                """,
                connection);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);
            command.Parameters.AddWithValue(
                "offset",
                NpgsqlDbType.Integer,
                offset);
            command.Parameters.AddWithValue(
                "limit",
                NpgsqlDbType.Integer,
                limit);

            var rows = new List<OrganisationProfileVersionRow>();

            await using (var reader =
                await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add(ReadRow(reader));
                }
            }

            var result = new List<EffectiveOrganisationProfile>();

            foreach (var row in rows)
            {
                result.Add(
                    await MaterializeAsync(
                            connection,
                            organisationProfileId,
                            row,
                            cancellationToken)
                        .ConfigureAwait(false));
            }

            return result;
        }

        private static async Task<EffectiveOrganisationProfile?> ReadOneAsync(
            NpgsqlConnection connection,
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionNumber version,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profile_versions v
                INNER JOIN organisation_profile.organisation_profiles p
                    ON p.organisation_profile_id =
                       v.organisation_profile_id
                WHERE v.organisation_profile_id =
                    @organisation_profile_id
                  AND v.profile_version =
                    @profile_version;
                """,
                connection);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);
            command.Parameters.AddWithValue(
                "profile_version",
                NpgsqlDbType.Bigint,
                version.Value);

            OrganisationProfileVersionRow? row = null;

            await using (var reader =
                await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    row = ReadRow(reader);
                }
            }

            return row is null
                ? null
                : await MaterializeAsync(
                        connection,
                        organisationProfileId,
                        row,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        private static async Task<EffectiveOrganisationProfile> MaterializeAsync(
            NpgsqlConnection connection,
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionRow row,
            CancellationToken cancellationToken)
        {
            var domains = await ReadDomainsAsync(
                    connection,
                    organisationProfileId,
                    row.Version,
                    cancellationToken)
                .ConfigureAwait(false);

            return new EffectiveOrganisationProfile(
                organisationProfileId,
                new OrganizationReference(
                    row.IdentityScopeId,
                    row.TenantId,
                    row.OrganizationId),
                row.Version,
                row.TemplateKey is null
                    ? null
                    : new OrganisationProfileTemplatePin(
                        row.TemplateKey.Value,
                        row.TemplateVersion
                            ?? throw new InvalidOperationException(
                                "Effective profile snapshot has an incomplete template pin.")),
                domains,
                row.ContentHash,
                row.ResolvedAt);
        }

        private static async Task<IReadOnlyList<OrganisationProfileDomainSelection>>
            ReadDomainsAsync(
                NpgsqlConnection connection,
                OrganisationProfileId organisationProfileId,
                OrganisationProfileVersionNumber version,
                CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT domain_key, domain_version
                FROM organisation_profile.organisation_profile_version_domains
                WHERE organisation_profile_id =
                    @organisation_profile_id
                  AND profile_version = @profile_version
                ORDER BY domain_key;
                """,
                connection);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);
            command.Parameters.AddWithValue(
                "profile_version",
                NpgsqlDbType.Bigint,
                version.Value);

            var domains =
                new List<OrganisationProfileDomainSelection>();

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                domains.Add(
                    new OrganisationProfileDomainSelection(
                        new DomainKey(reader.GetString(0)),
                        new DomainVersion(reader.GetInt32(1))));
            }

            return domains;
        }

        private static OrganisationProfileVersionRow ReadRow(
            NpgsqlDataReader reader) =>
            new(
                new OrganisationProfileVersionNumber(
                    reader.GetInt64(0)),
                reader.IsDBNull(1)
                    ? null
                    : new OrganisationProfileTemplateKey(
                        reader.GetString(1)),
                reader.IsDBNull(2)
                    ? null
                    : new OrganisationProfileTemplateVersionNumber(
                        reader.GetInt32(2)),
                new OrganisationProfileContentHash(
                    reader.GetString(3)),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetGuid(5),
                reader.GetGuid(6),
                reader.GetGuid(7));

    }
}
