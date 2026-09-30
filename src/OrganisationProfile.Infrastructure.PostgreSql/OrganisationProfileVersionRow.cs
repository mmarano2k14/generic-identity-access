using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed record OrganisationProfileVersionRow(
        OrganisationProfileVersionNumber Version,
        OrganisationProfileTemplateKey? TemplateKey,
        OrganisationProfileTemplateVersionNumber? TemplateVersion,
        OrganisationProfileContentHash ContentHash,
        DateTimeOffset ResolvedAt,
        Guid IdentityScopeId,
        Guid TenantId,
        Guid OrganizationId);
}
