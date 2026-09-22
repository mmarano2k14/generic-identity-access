using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents membership in an identity-scope administration group.</summary>
    public sealed record IdentityScopeAdministrationMemberResponse(Guid GroupId, Guid UserId)
    {
        /// <summary>Maps a membership.</summary>
        public static IdentityScopeAdministrationMemberResponse From(
            IdentityScopeAdministrationGroupMembership membership) =>
            new(membership.Group.GroupId, membership.Subject.UserId);
    }
}
