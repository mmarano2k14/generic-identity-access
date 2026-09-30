using OrganisationProfile.Domain;
using OrganisationProfileAggregate = global::OrganisationProfile.Domain.OrganisationProfile;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents mutable OrganisationProfile definition state.</summary>
    public sealed record OrganisationProfileResponse(
        Guid OrganisationProfileId,
        Guid IdentityScopeId,
        Guid TenantId,
        Guid OrganizationId,
        OrganisationProfileTemplatePinResponse? TemplatePin,
        OrganisationProfileStatus Status,
        long RowVersion,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        /// <summary>Maps a domain profile to the HTTP contract.</summary>
        public static OrganisationProfileResponse From(
            OrganisationProfileAggregate profile) =>
            new(
                profile.OrganisationProfileId.Value,
                profile.Organization.IdentityScopeId,
                profile.Organization.TenantId,
                profile.Organization.OrganizationId,
                profile.TemplatePin is null
                    ? null
                    : OrganisationProfileTemplatePinResponse.From(
                        profile.TemplatePin),
                profile.Status,
                profile.RowVersion,
                profile.CreatedAt,
                profile.UpdatedAt);
    }
}
