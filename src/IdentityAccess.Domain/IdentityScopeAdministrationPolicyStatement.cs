namespace IdentityAccess.Domain
{
    /// <summary>Capability-pattern statement attached to an identity-scope administration policy.</summary>
    public sealed class IdentityScopeAdministrationPolicyStatement
    {
        /// <summary>Gets the statement identifier.</summary>
        public Guid StatementId { get; }
        /// <summary>Gets the policy.</summary>
        public IdentityScopeAdministrationPolicyReference Policy { get; }
        /// <summary>Gets the pinned security model.</summary>
        public ApplicationSecurityModelReference Model { get; }
        /// <summary>Gets the capability pattern.</summary>
        public CapabilityPattern Pattern { get; }

        /// <summary>Initializes a scope-administration policy statement.</summary>
        public IdentityScopeAdministrationPolicyStatement(
            Guid statementId,
            IdentityScopeAdministrationPolicyReference policy,
            ApplicationSecurityModelReference model,
            CapabilityPattern pattern)
        {
            StatementId = ModelGuard.Identifier(statementId, nameof(statementId));
            ArgumentNullException.ThrowIfNull(policy);
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(pattern);

            if (policy.IdentityScopeId != model.IdentityScopeId || policy.Application != model.Application)
                throw new ArgumentException(
                    "Policy and security model must belong to the same identity scope and application.",
                    nameof(model));

            Policy = policy;
            Model = model;
            Pattern = pattern;
        }
    }
}
