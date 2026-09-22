namespace IdentityAccess.Domain
{
    /// <summary>An identity-scope administration policy scoped to one application.</summary>
    public sealed record IdentityScopeAdministrationPolicyReference
    {
        /// <summary>Gets the identity scope.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the policy identifier.</summary>
        public Guid PolicyId { get; }

        /// <summary>Initializes a scope-administration policy reference.</summary>
        public IdentityScopeAdministrationPolicyReference(Guid identityScopeId, ApplicationKey application, Guid policyId)
        {
            IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            Application = application;
            PolicyId = ModelGuard.Identifier(policyId, nameof(policyId));
        }
    }
}
