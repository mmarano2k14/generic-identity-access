using OrganisationProfile.Domain;

namespace OrganisationProfile.Infrastructure.PostgreSql
{
    internal sealed record OrganisationProfileLockState(
        Guid IdentityScopeId,
        Guid TenantId,
        Guid OrganizationId,
        OrganisationProfileTemplateKey? TemplateKey,
        OrganisationProfileTemplateVersionNumber? TemplateVersion,
        OrganisationProfileStatus Status,
        long RowVersion);
}
