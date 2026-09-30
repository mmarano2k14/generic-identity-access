using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Storage
{
    /// <summary>Persists immutable effective OrganisationProfile semantic snapshots.</summary>
    public interface IOrganisationProfileVersionStore
    {
        /// <summary>Gets one immutable effective profile version.</summary>
        Task<EffectiveOrganisationProfile?> GetAsync(
            OrganisationProfileId organisationProfileId,
            OrganisationProfileVersionNumber version,
            CancellationToken cancellationToken);

        /// <summary>Gets the latest immutable effective profile version.</summary>
        Task<EffectiveOrganisationProfile?> GetLatestAsync(
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken);

        /// <summary>Lists immutable versions using bounded paging.</summary>
        Task<IReadOnlyList<EffectiveOrganisationProfile>> ListAsync(
            OrganisationProfileId organisationProfileId,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>
        /// Appends a resolved version if semantic content changed.
        /// The parent profile RowVersion is revalidated atomically.
        /// </summary>
        Task<EffectiveOrganisationProfile> AppendResolvedAsync(
            Domain.OrganisationProfile profile,
            IEnumerable<OrganisationProfileDomainSelection> domains,
            OrganisationProfileContentHash contentHash,
            DateTimeOffset resolvedAt,
            CancellationToken cancellationToken);
    }
}
