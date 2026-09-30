using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed class PostgreSqlOrganisationProfileVersionWriter(
        NpgsqlDataSource dataSource,
        PostgreSqlOrganisationProfileVersionReader reader)
    {
        public async Task<EffectiveOrganisationProfile> AppendResolvedAsync(
            Domain.OrganisationProfile profile,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            OrganisationProfileContentHash contentHash,
            DateTimeOffset resolvedAt,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(domains);

            if (profile.RowVersion <= 0)
            {
                throw new ArgumentException(
                    "Resolved snapshots require a persisted OrganisationProfile.",
                    nameof(profile));
            }

            var ordered = domains
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (ordered
                .GroupBy(item => item.DomainKey)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Effective snapshot cannot contain duplicate DomainKey entries.",
                    nameof(domains));
            }

            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            var durable = await LockProfileAsync(
                    connection,
                    transaction,
                    profile.OrganisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (durable is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new OrganisationProfileNotFoundException(
                    $"OrganisationProfile '{profile.OrganisationProfileId}' no longer exists.");
            }

            ValidateDurableState(profile, durable);

            var latest = await ReadLatestIdentityAsync(
                    connection,
                    transaction,
                    profile.OrganisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (latest is not null &&
                latest.ContentHash == contentHash)
            {
                await transaction.CommitAsync(cancellationToken);

                return await reader.GetAsync(
                        profile.OrganisationProfileId,
                        latest.Version,
                        cancellationToken)
                    .ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        "Latest effective snapshot could not be reloaded.");
            }

            var nextVersion =
                new OrganisationProfileVersionNumber(
                    latest is null
                        ? 1
                        : checked(latest.Version.Value + 1));

            await InsertVersionAsync(
                    connection,
                    transaction,
                    profile,
                    nextVersion,
                    contentHash,
                    resolvedAt,
                    cancellationToken)
                .ConfigureAwait(false);

            await InsertDomainsAsync(
                    connection,
                    transaction,
                    profile.OrganisationProfileId,
                    nextVersion,
                    ordered,
                    cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken);

            return await reader.GetAsync(
                    profile.OrganisationProfileId,
                    nextVersion,
                    cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "Persisted effective snapshot could not be reloaded.");
        }

        private static async Task<OrganisationProfileLockState?> LockProfileAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT
                    identity_scope_id,
                    tenant_id,
                    organization_id,
                    template_key,
                    template_version,
                    status,
                    row_version
                FROM organisation_profile.organisation_profiles
                WHERE organisation_profile_id =
                    @organisation_profile_id
                FOR UPDATE;
                """,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            await using var result =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await result.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new OrganisationProfileLockState(
                result.GetGuid(0),
                result.GetGuid(1),
                result.GetGuid(2),
                result.IsDBNull(3)
                    ? null
                    : new OrganisationProfileTemplateKey(
                        result.GetString(3)),
                result.IsDBNull(4)
                    ? null
                    : new OrganisationProfileTemplateVersionNumber(
                        result.GetInt32(4)),
                (OrganisationProfileStatus)result.GetInt16(5),
                result.GetInt64(6));
        }

        private static void ValidateDurableState(
            Domain.OrganisationProfile profile,
            OrganisationProfileLockState durable)
        {
            if (durable.IdentityScopeId !=
                    profile.Organization.IdentityScopeId ||
                durable.TenantId !=
                    profile.Organization.TenantId ||
                durable.OrganizationId !=
                    profile.Organization.OrganizationId)
            {
                throw new OrganisationProfileIdentityConflictException(
                    "OrganisationProfile Organization identity is immutable.");
            }

            if (durable.RowVersion != profile.RowVersion)
            {
                throw new OrganisationProfileConcurrencyException(
                    $"OrganisationProfile expected row version {profile.RowVersion} " +
                    $"but durable row version is {durable.RowVersion}.");
            }

            if (durable.Status != OrganisationProfileStatus.Active ||
                profile.Status != OrganisationProfileStatus.Active)
            {
                throw new OrganisationProfileInactiveException(
                    "Only an active OrganisationProfile may produce a new effective snapshot.");
            }

            var durableTemplate =
                durable.TemplateKey is null
                    ? null
                    : new OrganisationProfileTemplatePin(
                        durable.TemplateKey.Value,
                        durable.TemplateVersion
                            ?? throw new InvalidOperationException(
                                "Durable profile has an incomplete template pin."));

            if (durableTemplate != profile.TemplatePin)
            {
                throw new OrganisationProfileConcurrencyException(
                    "OrganisationProfile template pin changed before snapshot persistence.");
            }
        }

        private static async Task<OrganisationProfileLatestVersionIdentity?> ReadLatestIdentityAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT profile_version, content_hash
                FROM organisation_profile.organisation_profile_versions
                WHERE organisation_profile_id =
                    @organisation_profile_id
                ORDER BY profile_version DESC
                LIMIT 1;
                """,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            await using var result =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await result.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new OrganisationProfileLatestVersionIdentity(
                new OrganisationProfileVersionNumber(
                    result.GetInt64(0)),
                new OrganisationProfileContentHash(
                    result.GetString(1)));
        }

        private static async Task InsertVersionAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Domain.OrganisationProfile profile,
            OrganisationProfileVersionNumber version,
            OrganisationProfileContentHash contentHash,
            DateTimeOffset resolvedAt,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO organisation_profile.organisation_profile_versions
                (
                    organisation_profile_id,
                    profile_version,
                    template_key,
                    template_version,
                    content_hash,
                    source_profile_row_version,
                    resolved_at
                )
                VALUES
                (
                    @organisation_profile_id,
                    @profile_version,
                    @template_key,
                    @template_version,
                    @content_hash,
                    @source_profile_row_version,
                    @resolved_at
                );
                """,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                profile.OrganisationProfileId.Value);
            command.Parameters.AddWithValue(
                "profile_version",
                NpgsqlDbType.Bigint,
                version.Value);
            command.Parameters.AddWithValue(
                "template_key",
                NpgsqlDbType.Varchar,
                profile.TemplatePin is null
                    ? DBNull.Value
                    : profile.TemplatePin.TemplateKey.Value);
            command.Parameters.AddWithValue(
                "template_version",
                NpgsqlDbType.Integer,
                profile.TemplatePin is null
                    ? DBNull.Value
                    : profile.TemplatePin.Version.Value);
            command.Parameters.AddWithValue(
                "content_hash",
                NpgsqlDbType.Char,
                contentHash.Value);
            command.Parameters.AddWithValue(
                "source_profile_row_version",
                NpgsqlDbType.Bigint,
                profile.RowVersion);
            command.Parameters.AddWithValue(
                "resolved_at",
                NpgsqlDbType.TimestampTz,
                resolvedAt);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertDomainsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionNumber version,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            CancellationToken cancellationToken)
        {
            foreach (var domain in domains)
            {
                await using var command = new NpgsqlCommand("""
                    INSERT INTO organisation_profile.organisation_profile_version_domains
                    (
                        organisation_profile_id,
                        profile_version,
                        domain_key,
                        domain_version
                    )
                    VALUES
                    (
                        @organisation_profile_id,
                        @profile_version,
                        @domain_key,
                        @domain_version
                    );
                    """,
                    connection,
                    transaction);

                command.Parameters.AddWithValue(
                    "organisation_profile_id",
                    NpgsqlDbType.Uuid,
                    organisationProfileId.Value);
                command.Parameters.AddWithValue(
                    "profile_version",
                    NpgsqlDbType.Bigint,
                    version.Value);
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

    }
}
