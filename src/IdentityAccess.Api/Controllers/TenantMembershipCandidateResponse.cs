using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exact-login candidate result for adding one tenant member.</summary>
    public sealed record TenantMembershipCandidateResponse(
        Guid UserId,
        string DisplayName,
        UserStatus UserStatus,
        Guid? ExistingMembershipId,
        MembershipStatus? ExistingMembershipStatus)
    {
        public static TenantMembershipCandidateResponse From(TenantMembershipCandidate value) =>
            new(value.UserId, value.DisplayName, value.UserStatus, value.ExistingMembershipId, value.ExistingMembershipStatus);
    }
}
