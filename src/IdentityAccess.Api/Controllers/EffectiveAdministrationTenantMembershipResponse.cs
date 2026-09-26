namespace IdentityAccess.Api.Controllers
{
    /// <summary>Returns one active tenant membership visible to the authenticated subject.</summary>
    public sealed record EffectiveAdministrationTenantMembershipResponse(Guid MembershipId, Guid TenantId);
}
