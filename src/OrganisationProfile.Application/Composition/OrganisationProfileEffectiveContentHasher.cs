using System.Security.Cryptography;
using System.Text;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Composition
{
    /// <summary>Computes deterministic semantic identity for an effective OrganisationProfile.</summary>
    public sealed class OrganisationProfileEffectiveContentHasher
    {
        /// <summary>Hashes template pin plus stable key-ordered effective domains.</summary>
        public OrganisationProfileContentHash Compute(
            OrganisationProfileTemplatePin? templatePin,
            IEnumerable<OrganisationProfileDomainSelection> domains)
        {
            ArgumentNullException.ThrowIfNull(domains);

            var ordered = domains
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (ordered
                .GroupBy(item => item.DomainKey)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Effective composition cannot contain duplicate DomainKey entries.",
                    nameof(domains));
            }

            var canonical = new StringBuilder()
                .Append("organisation-profile-effective-content/v1")
                .Append('\n')
                .Append("template=");

            if (templatePin is null)
            {
                canonical.Append("none");
            }
            else
            {
                canonical
                    .Append(templatePin.TemplateKey.Value)
                    .Append('@')
                    .Append(
                        templatePin.Version.Value.ToString(
                            System.Globalization.CultureInfo.InvariantCulture));
            }

            canonical.Append('\n');

            foreach (var domain in ordered)
            {
                canonical
                    .Append("domain=")
                    .Append(domain.DomainKey.Value)
                    .Append('@')
                    .Append(
                        domain.DomainVersion.Value.ToString(
                            System.Globalization.CultureInfo.InvariantCulture))
                    .Append('\n');
            }

            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(canonical.ToString()));

            return new OrganisationProfileContentHash(
                Convert.ToHexString(bytes).ToLowerInvariant());
        }
    }
}
