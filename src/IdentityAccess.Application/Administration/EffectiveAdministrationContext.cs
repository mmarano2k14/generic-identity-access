using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Projects the trusted subject and its tenant-visibility boundary for administration.</summary>
    public sealed record EffectiveAdministrationContext(
        SubjectReference Subject,
        ApplicationKey Application,
        AdministrationTenantVisibilityMode TenantVisibility,
        IReadOnlyList<AdministrationTenantMembershipReference> ActiveTenantMemberships);
}
