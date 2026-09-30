using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed class PostgreSqlOrganisationProfileTemplatePublicationWriter(
        NpgsqlDataSource dataSource,
        PostgreSqlOrganisationProfileTemplateVersionReader reader)
    {
        public Task<OrganisationProfileTemplateVersion?> PublishAsync(
            OrganisationProfileTemplateVersion published,
            CancellationToken cancellationToken) =>
            ChangeStateAsync(
                published,
                OrganisationProfileTemplateVersionStatus.Draft,
                cancellationToken);

        public Task<OrganisationProfileTemplateVersion?> RetireAsync(
            OrganisationProfileTemplateVersion retired,
            CancellationToken cancellationToken) =>
            ChangeStateAsync(
                retired,
                OrganisationProfileTemplateVersionStatus.Published,
                cancellationToken);

        private async Task<OrganisationProfileTemplateVersion?> ChangeStateAsync(
            OrganisationProfileTemplateVersion target,
            OrganisationProfileTemplateVersionStatus expectedStatus,
            CancellationToken cancellationToken)
        {
            if (target.RowVersion <= 0)
                throw new ArgumentException(
                    "Persisted template versions require a positive row version.",
                    nameof(target));

            await using var command = dataSource.CreateCommand("""
                UPDATE organisation_profile.organisation_profile_template_versions
                SET status = @target_status,
                    content_hash = @content_hash,
                    row_version = row_version + 1,
                    updated_at = @updated_at,
                    published_at = @published_at,
                    retired_at = @retired_at
                WHERE template_key = @template_key
                  AND template_version = @template_version
                  AND status = @expected_status
                  AND row_version = @row_version;
                """);

            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                target.TemplateKey.Value);
            command.Parameters.AddWithValue(
                "template_version",
                NpgsqlDbType.Integer,
                target.Version.Value);
            command.Parameters.AddWithValue(
                "target_status",
                NpgsqlDbType.Smallint,
                (short)target.Status);
            command.Parameters.AddWithValue(
                "expected_status",
                NpgsqlDbType.Smallint,
                (short)expectedStatus);
            command.Parameters.AddWithValue(
                "content_hash",
                NpgsqlDbType.Char,
                target.ContentHash is null
                    ? DBNull.Value
                    : target.ContentHash.Value.Value);
            command.Parameters.AddWithValue(
                "row_version",
                NpgsqlDbType.Bigint,
                target.RowVersion);
            command.Parameters.AddWithValue(
                "updated_at",
                NpgsqlDbType.TimestampTz,
                target.UpdatedAt);
            command.Parameters.AddWithValue(
                "published_at",
                NpgsqlDbType.TimestampTz,
                target.PublishedAt is null
                    ? DBNull.Value
                    : target.PublishedAt.Value);
            command.Parameters.AddWithValue(
                "retired_at",
                NpgsqlDbType.TimestampTz,
                target.RetiredAt is null
                    ? DBNull.Value
                    : target.RetiredAt.Value);

            try
            {
                var affected =
                    await command.ExecuteNonQueryAsync(cancellationToken);

                if (affected == 0)
                    return await ResolveMissAsync(
                            target,
                            cancellationToken)
                        .ConfigureAwait(false);
            }
            catch (PostgresException exception) when (
                IsImmutableViolation(exception))
            {
                throw new OrganisationProfileTemplateVersionImmutableException(
                    "Published or retired template content is immutable.",
                    exception);
            }

            return await reader.GetAsync(
                    target.TemplateKey,
                    target.Version,
                    cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "Updated template version could not be reloaded.");
        }

        private async Task<OrganisationProfileTemplateVersion?> ResolveMissAsync(
            OrganisationProfileTemplateVersion expected,
            CancellationToken cancellationToken)
        {
            var current = await reader.GetAsync(
                    expected.TemplateKey,
                    expected.Version,
                    cancellationToken)
                .ConfigureAwait(false);

            if (current is null) return null;

            if (current.RowVersion != expected.RowVersion)
                throw new OrganisationProfileTemplateVersionConcurrencyException(
                    $"Expected row version {expected.RowVersion}, durable {current.RowVersion}.");

            throw new OrganisationProfileTemplateVersionImmutableException(
                "Template-version lifecycle transition is not allowed.");
        }

        private static bool IsImmutableViolation(
            PostgresException exception) =>
            exception.SqlState == PostgresErrorCodes.CheckViolation &&
            exception.ConstraintName ==
                "ck_organisation_profile_template_version_immutable";
    }
}
