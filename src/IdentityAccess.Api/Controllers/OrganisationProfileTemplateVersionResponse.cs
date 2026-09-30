using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one Draft, Published, or Retired OrganisationProfile template version.</summary>
    public sealed record OrganisationProfileTemplateVersionResponse(
        string TemplateKey,
        int TemplateVersion,
        OrganisationProfileTemplateVersionStatus Status,
        IReadOnlyList<OrganisationProfileDomainSelectionResponse> Domains,
        string? ContentHash,
        long RowVersion,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? PublishedAt,
        DateTimeOffset? RetiredAt)
    {
        /// <summary>Maps one template-version state to the HTTP contract.</summary>
        public static OrganisationProfileTemplateVersionResponse From(
            OrganisationProfileTemplateVersion version) =>
            new(
                version.TemplateKey.Value,
                version.Version.Value,
                version.Status,
                version.Domains
                    .Select(OrganisationProfileDomainSelectionResponse.From)
                    .ToArray(),
                version.ContentHash?.Value,
                version.RowVersion,
                version.CreatedAt,
                version.UpdatedAt,
                version.PublishedAt,
                version.RetiredAt);
    }
}
