using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authorization
{
    /// <summary>
    /// Describes one capability granted by a published managed-policy binding to a tenant group.
    /// This projection is used for delegation checks and does not perform wildcard authorization.
    /// </summary>
    public sealed record GroupCapabilityGrant
    {
        public CapabilityPattern Pattern { get; }
        public ResourceScopeReference? TargetScope { get; }
        public bool IncludeDescendants { get; }

        public GroupCapabilityGrant(
            CapabilityPattern pattern,
            ResourceScopeReference? targetScope,
            bool includeDescendants)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            if (targetScope is null && includeDescendants)
                throw new ArgumentException("Tenant-wide grants cannot include descendants.", nameof(includeDescendants));

            Pattern = pattern;
            TargetScope = targetScope;
            IncludeDescendants = includeDescendants;
        }
    }
}
