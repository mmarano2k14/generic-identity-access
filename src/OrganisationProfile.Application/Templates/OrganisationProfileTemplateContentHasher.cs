using System.Security.Cryptography;
using System.Text;
using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Templates
{
    public sealed class OrganisationProfileTemplateContentHasher
    {
        public OrganisationProfileContentHash Compute(
            OrganisationProfileTemplateKey templateKey,
            OrganisationProfileTemplateVersionNumber version,
            IEnumerable<OrganisationProfileDomainSelection> domains)
        {
            ArgumentNullException.ThrowIfNull(domains);

            var ordered = domains
                .OrderBy(item => item.DomainKey.Value, StringComparer.Ordinal)
                .ToArray();

            if (ordered.GroupBy(item => item.DomainKey).Any(group => group.Count() > 1))
                throw new ArgumentException(
                    "Template content cannot contain duplicate DomainKey entries.",
                    nameof(domains));

            var canonical = new StringBuilder()
                .Append("organisation-profile-template-content/v1\n")
                .Append("template=").Append(templateKey.Value).Append('\n')
                .Append("version=")
                .Append(version.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append('\n');

            foreach (var domain in ordered)
            {
                canonical
                    .Append("domain=")
                    .Append(domain.DomainKey.Value)
                    .Append('@')
                    .Append(domain.DomainVersion.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Append('\n');
            }

            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(canonical.ToString()));

            return new OrganisationProfileContentHash(
                Convert.ToHexString(bytes).ToLowerInvariant());
        }
    }
}
