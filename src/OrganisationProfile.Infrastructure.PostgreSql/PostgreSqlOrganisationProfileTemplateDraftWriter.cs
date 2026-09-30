using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed class PostgreSqlOrganisationProfileTemplateDraftWriter(
        NpgsqlDataSource dataSource,
        PostgreSqlOrganisationProfileTemplateVersionReader reader)
    {
        public async Task<OrganisationProfileTemplateVersion> CreateAsync(
            OrganisationProfileTemplateVersion draft,
            CancellationToken cancellationToken)
        {
            RequireDraft(draft, persisted: false);

            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await using (var command = new NpgsqlCommand("""
                    INSERT INTO organisation_profile.organisation_profile_template_versions
                    (
                        template_key,
                        template_version,
                        status,
                        content_hash,
                        row_version,
                        created_at,
                        updated_at,
                        published_at,
                        retired_at
                    )
                    VALUES
                    (
                        @template_key,
                        @template_version,
                        1,
                        NULL,
                        1,
                        @created_at,
                        @updated_at,
                        NULL,
                        NULL
                    );
                    """,
                    connection,
                    transaction))
                {
                    AddIdentity(command, draft);
                    command.Parameters.AddWithValue(
                        "created_at",
                        NpgsqlDbType.TimestampTz,
                        draft.CreatedAt);
                    command.Parameters.AddWithValue(
                        "updated_at",
                        NpgsqlDbType.TimestampTz,
                        draft.UpdatedAt);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await InsertDomainsAsync(
                    connection,
                    transaction,
                    draft,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new OrganisationProfileTemplateVersionAlreadyExistsException(
                    $"Template version '{draft.TemplateKey}@{draft.Version}' already exists.",
                    exception);
            }
            catch (PostgresException exception) when (
                exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new OrganisationProfileTemplateNotFoundException(
                    $"Template '{draft.TemplateKey}' does not exist.",
                    exception);
            }

            return await reader.GetAsync(
                    draft.TemplateKey,
                    draft.Version,
                    cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "Persisted Draft could not be reloaded.");
        }

        public async Task<OrganisationProfileTemplateVersion?> ReplaceDomainsAsync(
            OrganisationProfileTemplateVersion draft,
            CancellationToken cancellationToken)
        {
            RequireDraft(draft, persisted: true);

            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await using var touch = new NpgsqlCommand("""
                    UPDATE organisation_profile.organisation_profile_template_versions
                    SET row_version = row_version + 1,
                        updated_at = @updated_at
                    WHERE template_key = @template_key
                      AND template_version = @template_version
                      AND status = 1
                      AND row_version = @row_version;
                    """,
                    connection,
                    transaction);

                AddIdentity(touch, draft);
                touch.Parameters.AddWithValue(
                    "updated_at",
                    NpgsqlDbType.TimestampTz,
                    draft.UpdatedAt);
                touch.Parameters.AddWithValue(
                    "row_version",
                    NpgsqlDbType.Bigint,
                    draft.RowVersion);

                var affected =
                    await touch.ExecuteNonQueryAsync(cancellationToken);

                if (affected == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return await ResolveMissAsync(
                            draft,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                await using (var delete = new NpgsqlCommand("""
                    DELETE FROM organisation_profile.organisation_profile_template_domains
                    WHERE template_key = @template_key
                      AND template_version = @template_version;
                    """,
                    connection,
                    transaction))
                {
                    AddIdentity(delete, draft);
                    await delete.ExecuteNonQueryAsync(cancellationToken);
                }

                await InsertDomainsAsync(
                    connection,
                    transaction,
                    draft,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (PostgresException exception) when (
                IsImmutableViolation(exception))
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new OrganisationProfileTemplateVersionImmutableException(
                    "Published or retired template content is immutable.",
                    exception);
            }

            return await reader.GetAsync(
                    draft.TemplateKey,
                    draft.Version,
                    cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "Updated Draft could not be reloaded.");
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

            if (current.Status != OrganisationProfileTemplateVersionStatus.Draft)
                throw new OrganisationProfileTemplateVersionImmutableException(
                    "Published or retired template content is immutable.");

            throw new OrganisationProfileTemplateVersionConcurrencyException(
                $"Expected row version {expected.RowVersion}, durable {current.RowVersion}.");
        }

        private static async Task InsertDomainsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            OrganisationProfileTemplateVersion version,
            CancellationToken cancellationToken)
        {
            foreach (var domain in version.Domains)
            {
                await using var command = new NpgsqlCommand("""
                    INSERT INTO organisation_profile.organisation_profile_template_domains
                    (
                        template_key,
                        template_version,
                        domain_key,
                        domain_version
                    )
                    VALUES
                    (
                        @template_key,
                        @template_version,
                        @domain_key,
                        @domain_version
                    );
                    """,
                    connection,
                    transaction);

                AddIdentity(command, version);
                command.Parameters.AddWithValue(
                    "domain_key",
                    NpgsqlDbType.Varchar,
                    domain.DomainKey.Value);
                command.Parameters.AddWithValue(
                    "domain_version",
                    NpgsqlDbType.Integer,
                    domain.DomainVersion.Value);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private static void RequireDraft(
            OrganisationProfileTemplateVersion draft,
            bool persisted)
        {
            ArgumentNullException.ThrowIfNull(draft);

            if (draft.Status != OrganisationProfileTemplateVersionStatus.Draft)
                throw new ArgumentException(
                    "Operation requires a Draft template version.",
                    nameof(draft));

            if (persisted && draft.RowVersion <= 0)
                throw new ArgumentException(
                    "Persisted Draft requires a positive row version.",
                    nameof(draft));

            if (!persisted && draft.RowVersion != 0)
                throw new ArgumentException(
                    "New Draft must use row version zero.",
                    nameof(draft));
        }

        private static void AddIdentity(
            NpgsqlCommand command,
            OrganisationProfileTemplateVersion version)
        {
            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                version.TemplateKey.Value);
            command.Parameters.AddWithValue(
                "template_version",
                NpgsqlDbType.Integer,
                version.Version.Value);
        }

        private static bool IsImmutableViolation(
            PostgresException exception) =>
            exception.SqlState == PostgresErrorCodes.CheckViolation &&
            exception.ConstraintName ==
                "ck_organisation_profile_template_domain_immutable";
    }
}
