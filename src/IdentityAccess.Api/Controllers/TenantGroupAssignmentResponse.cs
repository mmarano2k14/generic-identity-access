using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>One tenant-scoped assignment between a membership and a group.</summary>
    public sealed record TenantGroupAssignmentResponse(Guid GroupId, Guid TenantMembershipId, Guid UserId)
    {
        public static TenantGroupAssignmentResponse From(GroupMembership value) =>
            new(value.Group.GroupId, value.TenantMembershipId, value.Subject.UserId);
    }
}
