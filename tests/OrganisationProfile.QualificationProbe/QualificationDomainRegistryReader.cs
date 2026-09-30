using OrganisationProfile.Application.Registry;
using OrganisationProfile.Domain;

namespace OrganisationProfile.QualificationProbe
{
    internal sealed class QualificationDomainRegistryReader : IDomainRegistryReader
    {
        private readonly Dictionary<
            (DomainKey DomainKey, DomainVersion DomainVersion),
            DomainRegistryVersionState> _versions = new();

        public void Set(
            string domainKey,
            int domainVersion,
            DomainRegistryVersionStatus status)
        {
            var key = new DomainKey(domainKey);
            var version = new DomainVersion(domainVersion);

            _versions[(key, version)] =
                new DomainRegistryVersionState(
                    key,
                    version,
                    status);
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
