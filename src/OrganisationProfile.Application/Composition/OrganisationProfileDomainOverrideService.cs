using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Composition
{
    /// <summary>Owns Organization-specific domain override replacement only.</summary>
    public sealed class OrganisationProfileDomainOverrideService(
        IOrganisationProfileStore profileStore,
        IOrganisationProfileDomainOverrideStore overrideStore,
        OrganisationProfileDomainRegistryValidator domainValidator,
        IOrganisationProfileClock clock)
    {
        /// <summary>
        /// Replaces the complete override set under the parent profile's optimistic concurrency token.
        /// </summary>
        public async Task<Domain.OrganisationProfile?> ReplaceAsync(
            OrganisationProfileId organisationProfileId,
            long expectedProfileRowVersion,
            IEnumerable<OrganisationProfileDomainOverride> overrides,
            CancellationToken cancellationToken)
        {
            if (expectedProfileRowVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedProfileRowVersion));
            }

            ArgumentNullException.ThrowIfNull(overrides);

            var overrideArray = overrides
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (overrideArray
                .GroupBy(item => item.DomainKey)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Override set cannot contain duplicate DomainKey entries.",
                    nameof(overrides));
            }

            var profile = await profileStore.GetAsync(
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return null;
            }

            if (profile.Status != OrganisationProfileStatus.Active)
            {
                throw new OrganisationProfileInactiveException(
                    "Domain overrides can only be changed for an active OrganisationProfile.");
            }

            var enabledDomains = overrideArray
                .Where(item =>
                    item.Operation ==
                    OrganisationProfileDomainOverrideOperation.Enable)
                .Select(item =>
                    new OrganisationProfileDomainSelection(
                        item.DomainKey,
                        item.DomainVersion
                            ?? throw new InvalidOperationException(
                                "Enable override requires an explicit DomainVersion.")))
                .ToArray();

            await domainValidator.RequireSelectableAsync(
                    enabledDomains,
                    cancellationToken)
                .ConfigureAwait(false);

            var newRowVersion = await overrideStore.ReplaceAsync(
                    organisationProfileId,
                    expectedProfileRowVersion,
                    overrideArray,
                    clock.UtcNow,
                    cancellationToken)
                .ConfigureAwait(false);

            if (newRowVersion is null)
            {
                return null;
            }

            var updated = await profileStore.GetAsync(
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "Updated OrganisationProfile could not be reloaded.");

            if (updated.RowVersion != newRowVersion.Value)
            {
                throw new InvalidOperationException(
                    "Override replacement returned an unexpected profile RowVersion.");
            }

            return updated;
        }
    }
}
