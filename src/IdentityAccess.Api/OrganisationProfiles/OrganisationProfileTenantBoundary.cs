using OrganisationProfile.Domain;
using OrganisationProfileAggregate = global::OrganisationProfile.Domain.OrganisationProfile;

namespace IdentityAccess.Api.OrganisationProfiles
{
    /// <summary>Applies tenant/scope ownership checks before profile-id based operations execute.</summary>
    internal static class OrganisationProfileTenantBoundary
    {
        /// <summary>Returns true when the profile belongs to the route's trusted tenant boundary.</summary>
        public static bool Matches(
            OrganisationProfileAggregate profile,
            Guid identityScopeId,
            Guid tenantId) =>
            profile.Organization.IdentityScopeId == identityScopeId &&
            profile.Organization.TenantId == tenantId;

        /// <summary>Returns true when an immutable effective version belongs to the route boundary.</summary>
        public static bool Matches(
            EffectiveOrganisationProfile profile,
            Guid identityScopeId,
            Guid tenantId) =>
            profile.Organization.IdentityScopeId == identityScopeId &&
            profile.Organization.TenantId == tenantId;
    }
}
