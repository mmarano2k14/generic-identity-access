namespace IdentityAccess.Domain
{
    /// <summary>Binds one identity-scope administration group to one policy.</summary>
    public sealed record IdentityScopeAdministrationGroupPolicyBinding
    {
        /// <summary>Gets the administration group.</summary>
        public IdentityScopeAdministrationGroupReference Group { get; }
        /// <summary>Gets the administration policy.</summary>
        public IdentityScopeAdministrationPolicyReference Policy { get; }

        /// <summary>Initializes a scope-administration group-policy binding.</summary>
        public IdentityScopeAdministrationGroupPolicyBinding(
            IdentityScopeAdministrationGroupReference group,
            IdentityScopeAdministrationPolicyReference policy)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policy);

            if (group.IdentityScopeId != policy.IdentityScopeId || group.Application != policy.Application)
                throw new ArgumentException(
                    "Administration group and policy must belong to the same identity scope and application.",
                    nameof(policy));

            Group = group;
            Policy = policy;
        }
    }
}
