

namespace IdentityAccess.Domain
{

    /// <summary>
    /// Structural reference from a policy to one declared security-model version and one capability grant pattern.
    /// It is not a TRN and does not by itself grant permission.
    /// </summary>
    public sealed class PolicyStatement
    {
        /// <summary>Gets the statement identifier.</summary>
        public Guid StatementId { get; }
        /// <summary>Gets the policy.</summary>
        public PermissionPolicyReference Policy { get; }
        /// <summary>Gets the model.</summary>
        public ApplicationSecurityModelReference Model { get; }
        /// <summary>Gets the pattern.</summary>
        public CapabilityPattern Pattern { get; }

        /// <summary>Initializes a new instance of <see cref="PolicyStatement"/>.</summary>
        public PolicyStatement(Guid statementId, PermissionPolicyReference policy,
            ApplicationSecurityModelReference model, CapabilityPattern pattern)
        {
            StatementId = ModelGuard.Identifier(statementId, nameof(statementId));
            ArgumentNullException.ThrowIfNull(policy);
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(pattern);
            if (policy.Tenant.IdentityScopeId != model.IdentityScopeId)
                throw new ArgumentException("Policy and capability model must belong to the same identity scope.", nameof(model));
            if (policy.Application != model.Application)
                throw new ArgumentException("Policy and capability model must belong to the same application.", nameof(model));
            Policy = policy;
            Model = model;
            Pattern = pattern;
        }

        /// <summary>Initializes a new instance of <see cref="PolicyStatement"/>.</summary>
        public PolicyStatement(Guid statementId, PermissionPolicyReference policy,
            ApplicationSecurityModelReference model, CapabilityKey capability)
            : this(statementId, policy, model, new CapabilityPattern(capability))
        {
        }
    }
}
