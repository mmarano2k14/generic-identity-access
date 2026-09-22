

namespace IdentityAccess.Domain
{

    /// <summary>
    /// Structural group-to-policy binding. A null target is tenant-wide. A resource target may optionally
    /// include its descendants. Authorization still requires current server-side evaluation.
    /// </summary>
    public sealed class GroupPolicyBinding
    {
        /// <summary>Gets the group.</summary>
        public GroupReference Group { get; }
        /// <summary>Gets the policy.</summary>
        public PermissionPolicyReference Policy { get; }
        /// <summary>Gets the target scope.</summary>
        public ResourceScopeReference? TargetScope { get; }
        /// <summary>Gets the include descendants.</summary>
        public bool IncludeDescendants { get; }

        private GroupPolicyBinding(GroupReference group, PermissionPolicyReference policy,
            ResourceScopeReference? targetScope, bool includeDescendants)
        {
            EnsureSameBoundary(group, policy, targetScope);
            if (targetScope is null && includeDescendants)
                throw new ArgumentException("A tenant-wide binding cannot request descendant expansion.", nameof(includeDescendants));
            Group = group;
            Policy = policy;
            TargetScope = targetScope;
            IncludeDescendants = includeDescendants;
        }

        /// <summary>Creates group policy binding.</summary>
        public static GroupPolicyBinding Create(UserGroup group, PermissionPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policy);
            EnsureActive(group, policy);
            return new GroupPolicyBinding(group.Reference, policy.Reference, null, false);
        }

        /// <summary>Creates group policy binding.</summary>
        public static GroupPolicyBinding Create(UserGroup group, PermissionPolicy policy,
            ResourceScope targetScope, bool includeDescendants)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policy);
            ArgumentNullException.ThrowIfNull(targetScope);
            EnsureActive(group, policy);
            if (targetScope.Status != ResourceScopeStatus.Active)
                throw new InvalidOperationException("A new scoped policy binding requires an active resource scope.");
            return new GroupPolicyBinding(group.Reference, policy.Reference, targetScope.Reference, includeDescendants);
        }

        /// <summary>Rehydrates a persisted policy binding after storage invariants have been verified.</summary>
        public static GroupPolicyBinding Restore(GroupReference group, PermissionPolicyReference policy) =>
            new(group, policy, null, false);

        /// <summary>Rehydrates a persisted policy binding after storage invariants have been verified.</summary>
        public static GroupPolicyBinding Restore(GroupReference group, PermissionPolicyReference policy,
            ResourceScopeReference? targetScope, bool includeDescendants) =>
            new(group, policy, targetScope, includeDescendants);

        private static void EnsureActive(UserGroup group, PermissionPolicy policy)
        {
            EnsureSameBoundary(group.Reference, policy.Reference, null);
            if (group.Status != GroupStatus.Active || policy.Status != PolicyStatus.Active)
                throw new InvalidOperationException("A new policy binding requires an active group and policy.");
        }

        private static void EnsureSameBoundary(GroupReference group, PermissionPolicyReference policy,
            ResourceScopeReference? targetScope)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policy);
            if (group.Tenant != policy.Tenant || group.Application != policy.Application)
                throw new ArgumentException("Group and policy must belong to the same scoped tenant and application.", nameof(policy));
            if (targetScope is not null &&
                (targetScope.Tenant != group.Tenant || targetScope.Application != group.Application))
                throw new ArgumentException("The resource scope target must belong to the same tenant and application.", nameof(targetScope));
        }
    }
}
