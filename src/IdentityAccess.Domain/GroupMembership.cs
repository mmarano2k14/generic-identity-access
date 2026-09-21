namespace IdentityAccess.Domain;

/// <summary>Structural group membership. Account and access checks still belong to the server.</summary>
public sealed class GroupMembership
{
    public GroupReference Group { get; }
    public Guid TenantMembershipId { get; }
    public SubjectReference Subject { get; }

    private GroupMembership(GroupReference group, TenantMembership membership)
    {
        Group = group;
        TenantMembershipId = membership.MembershipId;
        Subject = membership.Subject;
    }

    public static GroupMembership Create(UserGroup group, TenantMembership membership)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(membership);
        if (group.Reference.Tenant != membership.Tenant)
            throw new ArgumentException("Group and membership must belong to the same scoped tenant.", nameof(membership));
        if (group.Status != GroupStatus.Active || membership.Status != MembershipStatus.Active)
            throw new InvalidOperationException("New group membership requires an active group and tenant membership.");
        return new GroupMembership(group.Reference, membership);
    }
}
