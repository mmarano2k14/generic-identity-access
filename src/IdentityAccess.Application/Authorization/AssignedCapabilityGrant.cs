using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authorization
{

    /// <summary>
    /// Provenance-rich capability assignment derived from current active directory and policy state.
    /// The pattern may contain one of the supported whole-segment wildcard forms.
    /// A null TargetScope means tenant-wide. Wildcard evaluation remains external.
    /// </summary>
    public sealed record AssignedCapabilityGrant
    {
        /// <summary>Gets the subject.</summary>
        public SubjectReference Subject { get; }
        /// <summary>Gets the tenant.</summary>
        public TenantReference Tenant { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the group.</summary>
        public GroupReference Group { get; }
        /// <summary>Gets the policy.</summary>
        public PermissionPolicyReference Policy { get; }
        /// <summary>Gets the statement identifier.</summary>
        public Guid StatementId { get; }
        /// <summary>Gets the model.</summary>
        public ApplicationSecurityModelReference Model { get; }
        /// <summary>Gets the pattern.</summary>
        public CapabilityPattern Pattern { get; }
        /// <summary>Gets the target scope.</summary>
        public ResourceScopeReference? TargetScope { get; }
        /// <summary>Gets the include descendants.</summary>
        public bool IncludeDescendants { get; }

        /// <summary>Initializes a new instance of <see cref="AssignedCapabilityGrant"/>.</summary>
        public AssignedCapabilityGrant(
            SubjectReference subject,
            TenantReference tenant,
            ApplicationKey application,
            GroupReference group,
            PermissionPolicyReference policy,
            Guid statementId,
            ApplicationSecurityModelReference model,
            CapabilityPattern pattern,
            ResourceScopeReference? targetScope = null,
            bool includeDescendants = false)
        {
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(policy);
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(pattern);
            if (statementId == Guid.Empty)
                throw new ArgumentException("Statement id must not be empty.", nameof(statementId));
            if (targetScope is null && includeDescendants)
                throw new ArgumentException("Tenant-wide grants cannot carry descendant expansion.", nameof(includeDescendants));

            if (subject.IdentityScopeId != tenant.IdentityScopeId ||
                group.Tenant != tenant || policy.Tenant != tenant || model.IdentityScopeId != tenant.IdentityScopeId)
                throw new ArgumentException("Assigned capability provenance must stay inside one identity scope and tenant.");

            if (group.Application != application || policy.Application != application || model.Application != application)
                throw new ArgumentException("Assigned capability provenance must stay inside one application.");

            if (targetScope is not null && (targetScope.Tenant != tenant || targetScope.Application != application))
                throw new ArgumentException("Assigned capability scope provenance must stay inside the same tenant and application.");

            Subject = subject;
            Tenant = tenant;
            Application = application;
            Group = group;
            Policy = policy;
            StatementId = statementId;
            Model = model;
            Pattern = pattern;
            TargetScope = targetScope;
            IncludeDescendants = includeDescendants;
        }

        /// <summary>Initializes a new instance of <see cref="AssignedCapabilityGrant"/>.</summary>
        public AssignedCapabilityGrant(
            SubjectReference subject,
            TenantReference tenant,
            ApplicationKey application,
            GroupReference group,
            PermissionPolicyReference policy,
            Guid statementId,
            ApplicationSecurityModelReference model,
            CapabilityKey capability,
            ResourceScopeReference? targetScope = null,
            bool includeDescendants = false)
            : this(subject, tenant, application, group, policy, statementId, model,
                new CapabilityPattern(capability), targetScope, includeDescendants)
        {
        }
    }
}
