using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Storage
{
    /// <summary>Persists the complete Organization-specific domain override set for one profile.</summary>
    public interface IOrganisationProfileDomainOverrideStore
    {
        /// <summary>Lists deterministic key-ordered overrides for one profile.</summary>
        Task<IReadOnlyList<OrganisationProfileDomainOverride>> ListAsync(
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Atomically replaces all overrides and advances the parent profile RowVersion.
        /// </summary>
        Task<long?> ReplaceAsync(
            OrganisationProfileId organisationProfileId,
            long expectedProfileRowVersion,
            IEnumerable<OrganisationProfileDomainOverride> overrides,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken);
    }
}
