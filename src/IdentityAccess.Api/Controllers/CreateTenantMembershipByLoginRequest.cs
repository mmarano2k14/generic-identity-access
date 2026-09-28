using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exact-login request for tenant-scoped membership creation without a global user-id picker.</summary>
    public sealed record CreateTenantMembershipByLoginRequest(
        string LoginIdentifier,
        MembershipStatus Status = MembershipStatus.Active,
        Guid MembershipId = default);
}
