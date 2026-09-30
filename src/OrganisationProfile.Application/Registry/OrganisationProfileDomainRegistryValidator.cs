using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Registry
{
    /// <summary>
    /// Applies OrganisationProfile's selection/resolution policy over an external Domain Registry reader.
    /// </summary>
    public sealed class OrganisationProfileDomainRegistryValidator(
        IDomainRegistryReader registry)
    {
        /// <summary>
        /// Requires every supplied domain version to exist and be Published before new selection.
        /// </summary>
        public Task RequireSelectableAsync(
            IEnumerable<OrganisationProfileDomainSelection> domains,
            CancellationToken cancellationToken) =>
            ValidateAsync(
                domains,
                requirePublished: true,
                cancellationToken);

        /// <summary>
        /// Requires every supplied domain version to remain resolvable.
        /// Published and Retired historical versions are resolvable.
        /// </summary>
        public Task RequireResolvableAsync(
            IEnumerable<OrganisationProfileDomainSelection> domains,
            CancellationToken cancellationToken) =>
            ValidateAsync(
                domains,
                requirePublished: false,
                cancellationToken);

        private async Task ValidateAsync(
            IEnumerable<OrganisationProfileDomainSelection> domains,
            bool requirePublished,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(domains);

            var ordered = domains
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            foreach (var domain in ordered)
            {
                var state = await registry.GetAsync(
                        domain.DomainKey,
                        domain.DomainVersion,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (state is null)
                {
                    throw new DomainRegistryVersionUnavailableException(
                        domain.DomainKey,
                        domain.DomainVersion,
                        $"Domain '{domain.DomainKey}@{domain.DomainVersion}' does not exist in the Domain Registry.");
                }

                if (requirePublished &&
                    state.Status != DomainRegistryVersionStatus.Published)
                {
                    throw new DomainRegistryVersionUnavailableException(
                        domain.DomainKey,
                        domain.DomainVersion,
                        $"Domain '{domain.DomainKey}@{domain.DomainVersion}' is not Published for new composition.");
                }

                if (!Enum.IsDefined(state.Status))
                {
                    throw new DomainRegistryVersionUnavailableException(
                        domain.DomainKey,
                        domain.DomainVersion,
                        $"Domain '{domain.DomainKey}@{domain.DomainVersion}' has an unsupported registry status.");
                }
            }
        }
    }
}
