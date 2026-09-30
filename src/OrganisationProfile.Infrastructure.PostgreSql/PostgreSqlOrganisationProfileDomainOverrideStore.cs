using Npgsql;
using NpgsqlTypes;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    /// <summary>PostgreSQL-backed full-set persistence for Organization-specific domain overrides.</summary>
    public sealed class PostgreSqlOrganisationProfileDomainOverrideStore(
        NpgsqlDataSource dataSource) : IOrganisationProfileDomainOverrideStore
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<OrganisationProfileDomainOverride>> ListAsync(
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken)
        {
            await using var command = dataSource.CreateCommand("""
                SELECT
                    domain_key,
                    operation,
                    domain_version
                FROM organisation_profile.organisation_profile_domain_overrides
                WHERE organisation_profile_id = @organisation_profile_id
                ORDER BY domain_key;
                """);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            var result =
                new List<OrganisationProfileDomainOverride>();

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var operation =
                    (OrganisationProfileDomainOverrideOperation)
                    reader.GetInt16(1);

                result.Add(
                    new OrganisationProfileDomainOverride(
                        new DomainKey(reader.GetString(0)),
                        reader.IsDBNull(2)
                            ? null
                            : new DomainVersion(reader.GetInt32(2)),
                        operation));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<long?> ReplaceAsync(
            OrganisationProfileId organisationProfileId,
            long expectedProfileRowVersion,
            IEnumerable<OrganisationProfileDomainOverride> overrides,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken)
        {
            if (expectedProfileRowVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedProfileRowVersion));
            }

            ArgumentNullException.ThrowIfNull(overrides);

            var ordered = overrides
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (ordered
                .GroupBy(item => item.DomainKey)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Override set cannot contain duplicate DomainKey entries.",
                    nameof(overrides));
            }

            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            var state = await LockProfileAsync(
                    connection,
                    transaction,
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (state is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (state.Value.Status != OrganisationProfileStatus.Active)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new OrganisationProfileInactiveException(
                    "Domain overrides can only be changed for an active OrganisationProfile.");
            }

            if (state.Value.RowVersion != expectedProfileRowVersion)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new OrganisationProfileConcurrencyException(
                    $"OrganisationProfile expected row version {expectedProfileRowVersion} " +
                    $"but durable row version is {state.Value.RowVersion}.");
            }

            await DeleteCurrentAsync(
                    connection,
                    transaction,
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            await InsertAsync(
                    connection,
                    transaction,
                    organisationProfileId,
                    ordered,
                    cancellationToken)
                .ConfigureAwait(false);

            var newRowVersion = await AdvanceProfileRowVersionAsync(
                    connection,
                    transaction,
                    organisationProfileId,
                    expectedProfileRowVersion,
                    updatedAt,
                    cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken);

            return newRowVersion;
        }

        private static async Task<(long RowVersion, OrganisationProfileStatus Status)?>
            LockProfileAsync(
                NpgsqlConnection connection,
                NpgsqlTransaction transaction,
                OrganisationProfileId organisationProfileId,
                CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                SELECT row_version, status
                FROM organisation_profile.organisation_profiles
                WHERE organisation_profile_id = @organisation_profile_id
                FOR UPDATE;
                """,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return (
                reader.GetInt64(0),
                (OrganisationProfileStatus)reader.GetInt16(1));
        }

        private static async Task DeleteCurrentAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                DELETE FROM organisation_profile.organisation_profile_domain_overrides
                WHERE organisation_profile_id = @organisation_profile_id;
                """,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            OrganisationProfileId organisationProfileId,
            IEnumerable<OrganisationProfileDomainOverride> overrides,
            CancellationToken cancellationToken)
        {
            foreach (var item in overrides)
            {
                await using var command = new NpgsqlCommand("""
                    INSERT INTO organisation_profile.organisation_profile_domain_overrides
                    (
                        organisation_profile_id,
                        domain_key,
                        operation,
                        domain_version
                    )
                    VALUES
                    (
                        @organisation_profile_id,
                        @domain_key,
                        @operation,
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
                    "domain_key",
                    NpgsqlDbType.Varchar,
                    item.DomainKey.Value);
                command.Parameters.AddWithValue(
                    "operation",
                    NpgsqlDbType.Smallint,
                    (short)item.Operation);
                command.Parameters.AddWithValue(
                    "domain_version",
                    NpgsqlDbType.Integer,
                    item.DomainVersion is null
                        ? DBNull.Value
                        : item.DomainVersion.Value.Value);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private static async Task<long> AdvanceProfileRowVersionAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            OrganisationProfileId organisationProfileId,
            long expectedProfileRowVersion,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("""
                UPDATE organisation_profile.organisation_profiles
                SET row_version = row_version + 1,
                    updated_at = @updated_at
                WHERE organisation_profile_id = @organisation_profile_id
                  AND row_version = @expected_row_version
                RETURNING row_version;
                """,
                connection,
                transaction);

            command.Parameters.AddWithValue(
                "organisation_profile_id",
                NpgsqlDbType.Uuid,
                organisationProfileId.Value);
            command.Parameters.AddWithValue(
                "expected_row_version",
                NpgsqlDbType.Bigint,
                expectedProfileRowVersion);
            command.Parameters.AddWithValue(
                "updated_at",
                NpgsqlDbType.TimestampTz,
                updatedAt);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is null or DBNull
                ? throw new OrganisationProfileConcurrencyException(
                    "OrganisationProfile RowVersion changed during override replacement.")
                : Convert.ToInt64(
                    result,
                    System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
