namespace IdentityAccess.Domain
{
    /// <summary>
    /// Tenant-scoped group binding to one immutable managed-policy version. The policy definition is
    /// identity-scope/application scoped; tenant ownership is carried only by the group and optional resource scope.
    /// </summary>
    public sealed class ManagedGroupPolicyBinding
    {
        /// <summary>Gets the tenant-scoped group receiving the policy.</summary>
        public GroupReference Group { get; }
        /// <summary>Gets the shared managed-policy version granted by this binding.</summary>
        public ManagedPolicyVersionReference PolicyVersion { get; }
        /// <summary>Gets the optional tenant-scoped resource target.</summary>
        public ResourceScopeReference? TargetScope { get; }
        /// <summary>Gets whether descendants of the selected resource scope are included.</summary>
        public bool IncludeDescendants { get; }

        private ManagedGroupPolicyBinding(
            GroupReference group,
            ManagedPolicyVersionReference policyVersion,
            ResourceScopeReference? targetScope,
            bool includeDescendants)
        {
            EnsureBoundary(group, policyVersion, targetScope);
            if (targetScope is null && includeDescendants)
                throw new ArgumentException("A tenant-wide binding cannot request descendant expansion.", nameof(includeDescendants));

            Group = group;
            PolicyVersion = policyVersion;
            TargetScope = targetScope;
            IncludeDescendants = includeDescendants;
        }

        /// <summary>Creates a new active group-to-managed-policy binding.</summary>
        public static ManagedGroupPolicyBinding Create(
            UserGroup group,
            ManagedPolicy policy,
            ManagedPolicyVersion version)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policy);
            ArgumentNullException.ThrowIfNull(version);
            EnsureActive(group, policy, version);
            return new ManagedGroupPolicyBinding(group.Reference, version.Reference, null, false);
        }

        /// <summary>Creates a new active scoped group-to-managed-policy binding.</summary>
        public static ManagedGroupPolicyBinding Create(
            UserGroup group,
            ManagedPolicy policy,
            ManagedPolicyVersion version,
            ResourceScope targetScope,
            bool includeDescendants)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policy);
            ArgumentNullException.ThrowIfNull(version);
            ArgumentNullException.ThrowIfNull(targetScope);
            EnsureActive(group, policy, version);
            if (targetScope.Status != ResourceScopeStatus.Active)
                throw new InvalidOperationException("A new scoped managed-policy binding requires an active resource scope.");

            return new ManagedGroupPolicyBinding(
                group.Reference,
                version.Reference,
                targetScope.Reference,
                includeDescendants);
        }

        /// <summary>Rehydrates a persisted managed-policy binding after storage invariants have been verified.</summary>
        public static ManagedGroupPolicyBinding Restore(
            GroupReference group,
            ManagedPolicyVersionReference policyVersion,
            ResourceScopeReference? targetScope,
            bool includeDescendants) =>
            new(group, policyVersion, targetScope, includeDescendants);

        private static void EnsureActive(UserGroup group, ManagedPolicy policy, ManagedPolicyVersion version)
        {
            if (group.Status != GroupStatus.Active || policy.Status != PolicyStatus.Active)
                throw new InvalidOperationException("A new managed-policy binding requires an active group and managed policy.");
            if (policy.Reference != version.Reference.Policy)
                throw new ArgumentException("The managed policy version must belong to the selected managed policy.", nameof(version));
            EnsureBoundary(group.Reference, version.Reference, null);
        }

        private static void EnsureBoundary(
            GroupReference group,
            ManagedPolicyVersionReference policyVersion,
            ResourceScopeReference? targetScope)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policyVersion);

            if (group.Tenant.IdentityScopeId != policyVersion.Policy.IdentityScopeId ||
                group.Application != policyVersion.Policy.Application)
            {
                throw new ArgumentException(
                    "Group and managed policy must belong to the same identity scope and application.",
                    nameof(policyVersion));
            }

            if (targetScope is not null &&
                (targetScope.Tenant != group.Tenant || targetScope.Application != group.Application))
            {
                throw new ArgumentException(
                    "The resource scope target must belong to the same tenant and application as the group.",
                    nameof(targetScope));
            }
        }
    }
}
