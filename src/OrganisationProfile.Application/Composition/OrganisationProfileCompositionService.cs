using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Composition
{
    /// <summary>
    /// Coordinates effective composition resolution and immutable semantic snapshot publication.
    /// </summary>
    public sealed class OrganisationProfileCompositionService(
        IOrganisationProfileStore profileStore,
        IOrganisationProfileTemplateVersionStore templateVersionStore,
        IOrganisationProfileDomainOverrideStore overrideStore,
        IOrganisationProfileVersionStore versionStore,
        OrganisationProfileCompositionResolver resolver,
        OrganisationProfileDomainRegistryValidator domainValidator,
        OrganisationProfileEffectiveContentHasher contentHasher,
        IOrganisationProfileClock clock)
    {
        /// <summary>
        /// Resolves effective version-pinned domains and appends a new semantic snapshot only when
        /// content changed.
        /// </summary>
        public async Task<EffectiveOrganisationProfile?> ResolveAndSnapshotAsync(
            OrganisationProfileId organisationProfileId,
            long expectedProfileRowVersion,
            CancellationToken cancellationToken)
        {
            if (expectedProfileRowVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedProfileRowVersion));
            }

            var profile = await profileStore.GetAsync(
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return null;
            }

            if (profile.RowVersion != expectedProfileRowVersion)
            {
                throw new OrganisationProfileConcurrencyException(
                    $"OrganisationProfile expected row version {expectedProfileRowVersion} " +
                    $"but loaded row version is {profile.RowVersion}.");
            }

            if (profile.Status != OrganisationProfileStatus.Active)
            {
                throw new OrganisationProfileInactiveException(
                    "Only an active OrganisationProfile may produce a new effective snapshot.");
            }

            var templateDomains =
                await ResolveTemplateDomainsAsync(
                        profile.TemplatePin,
                        cancellationToken)
                    .ConfigureAwait(false);

            var overrides = await overrideStore.ListAsync(
                    organisationProfileId,
                    cancellationToken)
                .ConfigureAwait(false);

            var effectiveDomains = resolver.Resolve(
                templateDomains,
                overrides);

            await domainValidator.RequireResolvableAsync(
                    effectiveDomains,
                    cancellationToken)
                .ConfigureAwait(false);

            var contentHash = contentHasher.Compute(
                profile.TemplatePin,
                effectiveDomains);

            return await versionStore.AppendResolvedAsync(
                    profile,
                    effectiveDomains,
                    contentHash,
                    clock.UtcNow,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<OrganisationProfileDomainSelection>>
            ResolveTemplateDomainsAsync(
                OrganisationProfileTemplatePin? templatePin,
                CancellationToken cancellationToken)
        {
            if (templatePin is null)
            {
                return Array.Empty<OrganisationProfileDomainSelection>();
            }

            var version = await templateVersionStore.GetAsync(
                    templatePin.TemplateKey,
                    templatePin.Version,
                    cancellationToken)
                .ConfigureAwait(false);

            if (version is null)
            {
                throw new OrganisationProfileTemplateVersionNotFoundException(
                    $"Pinned template version '{templatePin.TemplateKey}@{templatePin.Version}' does not exist.");
            }

            if (version.Status !=
                    OrganisationProfileTemplateVersionStatus.Published &&
                version.Status !=
                    OrganisationProfileTemplateVersionStatus.Retired)
            {
                throw new OrganisationProfileTemplateVersionNotPublishedException(
                    "Effective composition requires a Published or Retired historical template version.");
            }

            return version.Domains;
        }
    }
}
