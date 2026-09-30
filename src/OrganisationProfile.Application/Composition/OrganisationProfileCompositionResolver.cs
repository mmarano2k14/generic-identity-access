using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Composition
{
    /// <summary>
    /// Pure deterministic resolver: published template domains plus Organization-specific overrides.
    /// </summary>
    public sealed class OrganisationProfileCompositionResolver
    {
        /// <summary>Resolves a stable key-ordered effective domain composition.</summary>
        public IReadOnlyList<OrganisationProfileDomainSelection> Resolve(
            IEnumerable<OrganisationProfileDomainSelection> templateDomains,
            IEnumerable<OrganisationProfileDomainOverride> overrides)
        {
            ArgumentNullException.ThrowIfNull(templateDomains);
            ArgumentNullException.ThrowIfNull(overrides);

            var effective = new Dictionary<
                DomainKey,
                OrganisationProfileDomainSelection>();

            foreach (var domain in templateDomains)
            {
                if (!effective.TryAdd(domain.DomainKey, domain))
                {
                    throw new ArgumentException(
                        $"Template composition contains duplicate domain '{domain.DomainKey}'.",
                        nameof(templateDomains));
                }
            }

            var overrideArray = overrides.ToArray();

            if (overrideArray
                .GroupBy(item => item.DomainKey)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Organization overrides cannot contain duplicate DomainKey entries.",
                    nameof(overrides));
            }

            foreach (var item in overrideArray)
            {
                if (item.Operation ==
                    OrganisationProfileDomainOverrideOperation.Disable)
                {
                    effective.Remove(item.DomainKey);
                    continue;
                }

                effective[item.DomainKey] =
                    new OrganisationProfileDomainSelection(
                        item.DomainKey,
                        item.DomainVersion
                            ?? throw new InvalidOperationException(
                                "Enable override requires an explicit DomainVersion."));
            }

            return effective.Values
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
