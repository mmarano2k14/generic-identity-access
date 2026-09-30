using IdentityAccess.Api.Controllers;
using OrganisationProfile.Domain;

namespace IdentityAccess.Api.OrganisationProfiles
{
    /// <summary>Maps HTTP request values to validated OrganisationProfile domain values.</summary>
    internal static class OrganisationProfileRequestMapping
    {
        /// <summary>Maps an optional template key/version pair; both values must be present or absent.</summary>
        public static OrganisationProfileTemplatePin? TemplatePin(
            string? templateKey,
            int? templateVersion)
        {
            if (templateKey is null && templateVersion is null)
            {
                return null;
            }

            if (templateKey is null || templateVersion is null)
            {
                throw new ArgumentException(
                    "TemplateKey and TemplateVersion must both be supplied or both be null.");
            }

            return new OrganisationProfileTemplatePin(
                new OrganisationProfileTemplateKey(templateKey),
                new OrganisationProfileTemplateVersionNumber(
                    templateVersion.Value));
        }

        /// <summary>Maps a complete domain selection list.</summary>
        public static IReadOnlyList<OrganisationProfileDomainSelection> DomainSelections(
            IEnumerable<OrganisationProfileDomainSelectionRequest> domains)
        {
            ArgumentNullException.ThrowIfNull(domains);

            return domains
                .Select(item =>
                    new OrganisationProfileDomainSelection(
                        new DomainKey(item.DomainKey),
                        new DomainVersion(item.DomainVersion)))
                .ToArray();
        }

        /// <summary>Maps a complete domain override list.</summary>
        public static IReadOnlyList<OrganisationProfileDomainOverride> DomainOverrides(
            IEnumerable<OrganisationProfileDomainOverrideRequest> overrides)
        {
            ArgumentNullException.ThrowIfNull(overrides);

            return overrides
                .Select(item =>
                    new OrganisationProfileDomainOverride(
                        new DomainKey(item.DomainKey),
                        item.DomainVersion is null
                            ? null
                            : new DomainVersion(item.DomainVersion.Value),
                        item.Operation))
                .ToArray();
        }
    }
}
