using OrganisationProfile.Application.Registry;
using OrganisationProfile.Domain;

namespace IdentityAccess.Api.OrganisationProfiles
{
    /// <summary>
    /// Fail-closed fallback used until a concrete external Domain Registry reader is registered.
    /// </summary>
    internal sealed class UnavailableDomainRegistryReader : IDomainRegistryReader
    {
        /// <inheritdoc />
        public Task<DomainRegistryVersionState?> GetAsync(
            DomainKey domainKey,
            DomainVersion domainVersion,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = domainKey;
            _ = domainVersion;

            return Task.FromResult<DomainRegistryVersionState?>(null);
        }
    }
}
