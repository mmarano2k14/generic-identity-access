using OrganisationProfile.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents one immutable resolved OrganisationProfile semantic version.</summary>
    public sealed record EffectiveOrganisationProfileResponse(
        Guid OrganisationProfileId,
        Guid IdentityScopeId,
        Guid TenantId,
        Guid OrganizationId,
        long Version,
        OrganisationProfileTemplatePinResponse? TemplatePin,
        IReadOnlyList<OrganisationProfileDomainSelectionResponse> Domains,
        string ContentHash,
        DateTimeOffset ResolvedAt)
    {
        /// <summary>Maps one immutable effective profile to the HTTP contract.</summary>
        public static EffectiveOrganisationProfileResponse From(
            EffectiveOrganisationProfile profile) =>
            new(
                profile.OrganisationProfileId.Value,
                profile.Organization.IdentityScopeId,
                profile.Organization.TenantId,
                profile.Organization.OrganizationId,
                profile.Version.Value,
                profile.TemplatePin is null
                    ? null
                    : OrganisationProfileTemplatePinResponse.From(
                        profile.TemplatePin),
                profile.Domains
                    .Select(OrganisationProfileDomainSelectionResponse.From)
                    .ToArray(),
                profile.ContentHash.Value,
                profile.ResolvedAt);
    }
}
