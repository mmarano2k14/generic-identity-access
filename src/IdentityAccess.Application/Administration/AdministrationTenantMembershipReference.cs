using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>References one active tenant membership visible to the authenticated subject.</summary>
    public sealed record AdministrationTenantMembershipReference(Guid MembershipId, TenantReference Tenant);
}
