using Npgsql;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    public sealed class PostgreSqlOrganisationProfileTemplateVersionStore :
        IOrganisationProfileTemplateVersionStore
    {
        private readonly PostgreSqlOrganisationProfileTemplateVersionReader _reader;
        private readonly PostgreSqlOrganisationProfileTemplateDraftWriter _draftWriter;
        private readonly PostgreSqlOrganisationProfileTemplatePublicationWriter _publicationWriter;

        public PostgreSqlOrganisationProfileTemplateVersionStore(
            NpgsqlDataSource dataSource)
        {
            _reader =
                new PostgreSqlOrganisationProfileTemplateVersionReader(
                    dataSource);

            _draftWriter =
                new PostgreSqlOrganisationProfileTemplateDraftWriter(
                    dataSource,
                    _reader);

            _publicationWriter =
                new PostgreSqlOrganisationProfileTemplatePublicationWriter(
                    dataSource,
                    _reader);
        }

        public Task<OrganisationProfileTemplateVersion?> GetAsync(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            CancellationToken cancellationToken) =>
            _reader.GetAsync(
                templateKey,
                version,
                cancellationToken);

        public Task<IReadOnlyList<OrganisationProfileTemplateVersion>> ListAsync(
            OrganisationProfileTemplateKey templateKey,
            CancellationToken cancellationToken) =>
            _reader.ListAsync(
                templateKey,
                cancellationToken);

        public Task<OrganisationProfileTemplateVersion> CreateDraftAsync(
            OrganisationProfileTemplateVersion draft,
            CancellationToken cancellationToken) =>
            _draftWriter.CreateAsync(
                draft,
                cancellationToken);

        public Task<OrganisationProfileTemplateVersion?> ReplaceDraftDomainsAsync(
            OrganisationProfileTemplateVersion draft,
            CancellationToken cancellationToken) =>
            _draftWriter.ReplaceDomainsAsync(
                draft,
                cancellationToken);

        public Task<OrganisationProfileTemplateVersion?> PublishAsync(
            OrganisationProfileTemplateVersion published,
            CancellationToken cancellationToken) =>
            _publicationWriter.PublishAsync(
                published,
                cancellationToken);

        public Task<OrganisationProfileTemplateVersion?> RetireAsync(
            OrganisationProfileTemplateVersion retired,
            CancellationToken cancellationToken) =>
            _publicationWriter.RetireAsync(
                retired,
                cancellationToken);
    }
}
