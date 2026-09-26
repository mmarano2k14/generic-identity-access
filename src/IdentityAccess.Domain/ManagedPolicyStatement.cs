namespace IdentityAccess.Domain
{
    /// <summary>Capability statement belonging to one managed-policy version.</summary>
    public sealed class ManagedPolicyStatement
    {
        /// <summary>Gets the statement identifier.</summary>
        public Guid StatementId { get; }
        /// <summary>Gets the managed policy version.</summary>
        public ManagedPolicyVersionReference PolicyVersion { get; }
        /// <summary>Gets the application security model.</summary>
        public ApplicationSecurityModelReference Model { get; }
        /// <summary>Gets the capability grant pattern.</summary>
        public CapabilityPattern Pattern { get; }

        /// <summary>Initializes a new instance of <see cref="ManagedPolicyStatement"/>.</summary>
        public ManagedPolicyStatement(
            Guid statementId,
            ManagedPolicyVersionReference policyVersion,
            ApplicationSecurityModelReference model,
            CapabilityPattern pattern)
        {
            StatementId = ModelGuard.Identifier(statementId, nameof(statementId));
            ArgumentNullException.ThrowIfNull(policyVersion);
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(pattern);
            if (policyVersion.Policy.IdentityScopeId != model.IdentityScopeId)
                throw new ArgumentException("Managed policy and security model must belong to the same identity scope.", nameof(model));
            if (policyVersion.Policy.Application != model.Application)
                throw new ArgumentException("Managed policy and security model must belong to the same application.", nameof(model));
            PolicyVersion = policyVersion;
            Model = model;
            Pattern = pattern;
        }
    }
}
