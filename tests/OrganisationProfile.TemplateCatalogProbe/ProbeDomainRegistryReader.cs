using OrganisationProfile.Application.Registry;
using OrganisationProfile.Domain;

namespace OrganisationProfile.TemplateCatalogProbe
{
    internal sealed class ProbeDomainRegistryReader : IDomainRegistryReader
    {
        private readonly Dictionary<
            (DomainKey DomainKey, DomainVersion DomainVersion),
            DomainRegistryVersionState> _versions = new();

        public void AddPublished(
            string domainKey,
            int domainVersion)
        {
            var key = new DomainKey(domainKey);
            var version = new DomainVersion(domainVersion);

            _versions[(key, version)] =
                new DomainRegistryVersionState(
                    key,
                    version,
                    DomainRegistryVersionStatus.Published);
        }

        public Task<DomainRegistryVersionState?> GetAsync(
            DomainKey domainKey,
            DomainVersion domainVersion,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _versions.TryGetValue(
                (domainKey, domainVersion),
                out var state);

            return Task.FromResult(state);
        }
    }
}
