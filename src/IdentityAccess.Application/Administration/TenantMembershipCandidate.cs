using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Exact-login tenant-membership candidate without exposing a browsable identity directory.</summary>
    public sealed record TenantMembershipCandidate(
        Guid UserId,
        string DisplayName,
        UserStatus UserStatus,
        Guid? ExistingMembershipId,
        MembershipStatus? ExistingMembershipStatus);
}
