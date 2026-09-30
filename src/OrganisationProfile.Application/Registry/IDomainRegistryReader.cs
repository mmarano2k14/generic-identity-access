using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Registry
{
    /// <summary>
    /// Narrow read-only boundary to the external Domain Registry.
    /// OrganisationProfile never owns Domain Registry persistence.
    /// </summary>
    public interface IDomainRegistryReader
    {
        /// <summary>Gets one exact version-pinned domain, or null when it is unknown.</summary>
        Task<DomainRegistryVersionState?> GetAsync(
            DomainKey domainKey,
            DomainVersion domainVersion,
            CancellationToken cancellationToken);
    }
}
