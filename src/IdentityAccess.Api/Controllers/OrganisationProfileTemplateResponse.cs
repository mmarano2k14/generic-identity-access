using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one reusable OrganisationProfile template definition.</summary>
    public sealed record OrganisationProfileTemplateResponse(
        string TemplateKey,
        string DisplayName,
        OrganisationProfileTemplateStatus Status,
        long RowVersion,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        /// <summary>Maps one template definition to the HTTP contract.</summary>
        public static OrganisationProfileTemplateResponse From(
            OrganisationProfileTemplate template) =>
            new(
                template.TemplateKey.Value,
                template.DisplayName,
                template.Status,
                template.RowVersion,
                template.CreatedAt,
                template.UpdatedAt);
    }
}
