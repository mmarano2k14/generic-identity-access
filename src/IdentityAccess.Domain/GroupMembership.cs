

namespace IdentityAccess.Domain
{

    /// <summary>Structural group membership. Account and access checks still belong to the server.</summary>
    public sealed class GroupMembership
    {
        /// <summary>Gets the group.</summary>
        public GroupReference Group { get; }
        /// <summary>Gets the tenant membership identifier.</summary>
        public Guid TenantMembershipId { get; }
        /// <summary>Gets the subject.</summary>
        public SubjectReference Subject { get; }

        private GroupMembership(GroupReference group, Guid tenantMembershipId, SubjectReference subject)
        {
            Group = group;
            TenantMembershipId = tenantMembershipId;
            Subject = subject;
        }

        /// <summary>Creates group membership.</summary>
        public static GroupMembership Create(UserGroup group, TenantMembership membership)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(membership);
            if (group.Reference.Tenant != membership.Tenant)
                throw new ArgumentException("Group and membership must belong to the same scoped tenant.", nameof(membership));
            if (group.Status != GroupStatus.Active || membership.Status != MembershipStatus.Active)
                throw new InvalidOperationException("New group membership requires an active group and tenant membership.");
            return new GroupMembership(group.Reference, membership.MembershipId, membership.Subject);
        }
        /// <summary>
        /// Rehydrates a persisted membership edge. This validates identity structure only and is not an
        /// authorization decision; callers must evaluate current account, tenant, membership and group state.
        /// </summary>
        public static GroupMembership Restore(GroupReference group, Guid tenantMembershipId, SubjectReference subject)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(subject);
            if (tenantMembershipId == Guid.Empty)
                throw new ArgumentException("Membership id must not be empty.", nameof(tenantMembershipId));
            if (group.Tenant.IdentityScopeId != subject.IdentityScopeId)
                throw new ArgumentException("Group and subject must belong to the same identity scope.", nameof(subject));
            return new GroupMembership(group, tenantMembershipId, subject);
        }

    }
}
