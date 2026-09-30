using Npgsql;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    /// <summary>Composes focused immutable-version reader and writer responsibilities.</summary>
    public sealed class PostgreSqlOrganisationProfileVersionStore :
        IOrganisationProfileVersionStore
    {
        private readonly PostgreSqlOrganisationProfileVersionReader _reader;
        private readonly PostgreSqlOrganisationProfileVersionWriter _writer;

        public PostgreSqlOrganisationProfileVersionStore(
            NpgsqlDataSource dataSource)
        {
            _reader =
                new PostgreSqlOrganisationProfileVersionReader(dataSource);

            _writer =
                new PostgreSqlOrganisationProfileVersionWriter(
                    dataSource,
                    _reader);
        }

        /// <inheritdoc />
        public Task<EffectiveOrganisationProfile?> GetAsync(
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionNumber version,
            CancellationToken cancellationToken) =>
            _reader.GetAsync(
                organisationProfileId,
                version,
                cancellationToken);

        /// <inheritdoc />
        public Task<EffectiveOrganisationProfile?> GetLatestAsync(
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken) =>
            _reader.GetLatestAsync(
                organisationProfileId,
                cancellationToken);

        /// <inheritdoc />
        public Task<IReadOnlyList<EffectiveOrganisationProfile>> ListAsync(
            OrganisationProfileId organisationProfileId,
            int offset,
            int limit,
            CancellationToken cancellationToken) =>
            _reader.ListAsync(
                organisationProfileId,
                offset,
                limit,
                cancellationToken);

        /// <inheritdoc />
        public Task<EffectiveOrganisationProfile> AppendResolvedAsync(
            Domain.OrganisationProfile profile,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            OrganisationProfileContentHash contentHash,
            DateTimeOffset resolvedAt,
            CancellationToken cancellationToken) =>
            _writer.AppendResolvedAsync(
                profile,
                domains,
                contentHash,
                resolvedAt,
                cancellationToken);
    }
}
