using IdentityAccess.Application.Administration;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Returns the safe effective administration context derived by the trusted API.</summary>
    public sealed record EffectiveAdministrationContextResponse(
        Guid IdentityScopeId,
        Guid UserId,
        string ApplicationKey,
        string TenantVisibility,
        IReadOnlyList<EffectiveAdministrationTenantMembershipResponse> ActiveTenantMemberships)
    {
        /// <summary>Maps the application projection without exposing credentials or session secrets.</summary>
        public static EffectiveAdministrationContextResponse From(EffectiveAdministrationContext context) =>
            new(
                context.Subject.IdentityScopeId,
                context.Subject.UserId,
                context.Application.Value,
                context.TenantVisibility == AdministrationTenantVisibilityMode.ScopeWide
                    ? "scope-wide"
                    : "membership-limited",
                context.ActiveTenantMemberships
                    .Select(membership => new EffectiveAdministrationTenantMembershipResponse(
                        membership.MembershipId,
                        membership.Tenant.TenantId))
                    .ToArray());
    }
}
