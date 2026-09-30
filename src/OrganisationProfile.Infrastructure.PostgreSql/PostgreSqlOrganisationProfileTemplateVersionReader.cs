using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed class PostgreSqlOrganisationProfileTemplateVersionReader(
        NpgsqlDataSource dataSource)
    {
        private const string Projection = """
            template_key,
            template_version,
            status,
            content_hash,
            row_version,
            created_at,
            updated_at,
            published_at,
            retired_at
            """;

        public async Task<OrganisationProfileTemplateVersion?> GetAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            CancellationToken cancellationToken)
        {
            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await ReadOneAsync(
                    connection,
                    templateKey,
                    version,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<OrganisationProfileTemplateVersion>> ListAsync(
            OrganisationProfileTemplateKey templateKey,
            CancellationToken cancellationToken)
        {
            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using var command = new NpgsqlCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profile_template_versions
                WHERE template_key = @template_key
                ORDER BY template_version;
                """,
                connection);

            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                templateKey.Value);

            var rows = new List<OrganisationProfileTemplateVersionRow>();

            await using (var reader =
                await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    rows.Add(ReadVersionRow(reader));
            }

            var result =
                new List<OrganisationProfileTemplateVersion>();

            foreach (var row in rows)
            {
                result.Add(
                    await MaterializeAsync(
                            connection,
                            row,
                            cancellationToken)
                        .ConfigureAwait(false));
            }

            return result;
        }

        private async Task<OrganisationProfileTemplateVersion?> ReadOneAsync(
            NpgsqlConnection connection,
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profile_template_versions
                WHERE template_key = @template_key
                  AND template_version = @template_version;
                """,
                connection);

            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                templateKey.Value);
            command.Parameters.AddWithValue(
                "template_version",
                NpgsqlDbType.Integer,
                version.Value);

            OrganisationProfileTemplateVersionRow? row = null;

            await using (var reader =
                await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                    row = ReadVersionRow(reader);
            }

            return row is null
                ? null
                : await MaterializeAsync(
                        connection,
                        row,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        private static async Task<OrganisationProfileTemplateVersion> MaterializeAsync(
            NpgsqlConnection connection,
            OrganisationProfileTemplateVersionRow row,
            CancellationToken cancellationToken)
        {
            var domains = await ReadDomainsAsync(
                    connection,
                    row.TemplateKey,
                    row.Version,
                    cancellationToken)
                .ConfigureAwait(false);

            return new OrganisationProfileTemplateVersion(
                row.TemplateKey,
                row.Version,
                row.Status,
                domains,
                row.ContentHash,
                row.RowVersion,
                row.CreatedAt,
                row.UpdatedAt,
                row.PublishedAt,
                row.RetiredAt);
        }

        private static async Task<IReadOnlyList<OrganisationProfileDomainSelection>>
            ReadDomainsAsync(
                NpgsqlConnection connection,
                OrganisationProfileTemplateKey templateKey,
                OrganisationProfileTemplateVersionNumber version,
                CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT domain_key, domain_version
                FROM organisation_profile.organisation_profile_template_domains
                WHERE template_key = @template_key
                  AND template_version = @template_version
                ORDER BY domain_key;
                """,
                connection);

            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                templateKey.Value);
            command.Parameters.AddWithValue(
                "template_version",
                NpgsqlDbType.Integer,
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

        private static OrganisationProfileTemplateVersionRow ReadVersionRow(
            NpgsqlDataReader reader) =>
            new(
                new OrganisationProfileTemplateKey(reader.GetString(0)),
                new OrganisationProfileTemplateVersionNumber(reader.GetInt32(1)),
                (OrganisationProfileTemplateVersionStatus)reader.GetInt16(2),
                reader.IsDBNull(3)
                    ? null
                    : new OrganisationProfileContentHash(reader.GetString(3)),
                reader.GetInt64(4),
                reader.GetFieldValue<DateTimeOffset>(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.IsDBNull(7)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(7),
                reader.IsDBNull(8)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(8));

    }
}
