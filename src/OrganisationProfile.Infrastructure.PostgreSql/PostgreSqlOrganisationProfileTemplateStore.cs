using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    public sealed class PostgreSqlOrganisationProfileTemplateStore(
        NpgsqlDataSource dataSource) : IOrganisationProfileTemplateStore
    {
        private const string Projection = """
            template_key,
            display_name,
            status,
            row_version,
            created_at,
            updated_at
            """;

        public async Task<OrganisationProfileTemplate?> GetAsync(
            OrganisationProfileTemplateKey templateKey,
            CancellationToken cancellationToken)
        {
            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profile_templates
                WHERE template_key = @template_key;
                """);

            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                templateKey.Value);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            return await reader.ReadAsync(cancellationToken)
                ? ReadTemplate(reader)
                : null;
        }

        public async Task<IReadOnlyList<OrganisationProfileTemplate>> ListAsync(
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (limit is < 1 or > 500)
                throw new ArgumentOutOfRangeException(nameof(limit));

            await using var command = dataSource.CreateCommand($"""
                SELECT {Projection}
                FROM organisation_profile.organisation_profile_templates
                ORDER BY template_key
                OFFSET @offset
                LIMIT @limit;
                """);

            command.Parameters.AddWithValue(
                "offset",
                NpgsqlDbType.Integer,
                offset);
            command.Parameters.AddWithValue(
                "limit",
                NpgsqlDbType.Integer,
                limit);

            var templates = new List<OrganisationProfileTemplate>();

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
                templates.Add(ReadTemplate(reader));

            return templates;
        }

        public async Task<OrganisationProfileTemplate> CreateAsync(
            OrganisationProfileTemplate template,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(template);

            if (template.RowVersion != 0)
                throw new ArgumentException(
                    "New template definitions must use row version zero.",
                    nameof(template));

            await using var command = dataSource.CreateCommand($"""
                INSERT INTO organisation_profile.organisation_profile_templates
                (
                    template_key,
                    display_name,
                    status,
                    row_version,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @template_key,
                    @display_name,
                    @status,
                    1,
                    @created_at,
                    @updated_at
                )
                RETURNING {Projection};
                """);

            AddParameters(command, template, false);

            try
            {
                await using var reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException(
                        "PostgreSQL did not return the created template.");

                return ReadTemplate(reader);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new OrganisationProfileTemplateAlreadyExistsException(
                    $"Template '{template.TemplateKey}' already exists.",
                    exception);
            }
        }

        public async Task<OrganisationProfileTemplate?> UpdateAsync(
            OrganisationProfileTemplate template,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(template);

            if (template.RowVersion <= 0)
                throw new ArgumentException(
                    "Persisted template definitions require a positive row version.",
                    nameof(template));

            await using var command = dataSource.CreateCommand($"""
                UPDATE organisation_profile.organisation_profile_templates
                SET display_name = @display_name,
                    status = @status,
                    row_version = row_version + 1,
                    updated_at = @updated_at
                WHERE template_key = @template_key
                  AND row_version = @row_version
                RETURNING {Projection};
                """);

            AddParameters(command, template, true);

            await using (var reader =
                await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                    return ReadTemplate(reader);
            }

            var current = await GetAsync(
                    template.TemplateKey,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null) return null;

            throw new OrganisationProfileTemplateConcurrencyException(
                $"Template expected row version {template.RowVersion} " +
                $"but durable row version is {current.RowVersion}.");
        }

        private static OrganisationProfileTemplate ReadTemplate(
            NpgsqlDataReader reader) =>
            new(
                new OrganisationProfileTemplateKey(reader.GetString(0)),
                reader.GetString(1),
                (OrganisationProfileTemplateStatus)reader.GetInt16(2),
                reader.GetInt64(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetFieldValue<DateTimeOffset>(5));

        private static void AddParameters(
            NpgsqlCommand command,
            OrganisationProfileTemplate template,
            bool includeRowVersion)
        {
            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                template.TemplateKey.Value);
            command.Parameters.AddWithValue(
                "display_name",
                NpgsqlDbType.Varchar,
                template.DisplayName);
            command.Parameters.AddWithValue(
                "status",
                NpgsqlDbType.Smallint,
                (short)template.Status);
            command.Parameters.AddWithValue(
                "created_at",
                NpgsqlDbType.TimestampTz,
                template.CreatedAt);
            command.Parameters.AddWithValue(
                "updated_at",
                NpgsqlDbType.TimestampTz,
                template.UpdatedAt);

            if (includeRowVersion)
            {
                command.Parameters.AddWithValue(
                    "row_version",
                    NpgsqlDbType.Bigint,
                    template.RowVersion);
            }
        }
    }
}
