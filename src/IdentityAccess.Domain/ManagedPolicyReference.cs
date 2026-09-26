namespace IdentityAccess.Domain
{
    /// <summary>
    /// Identity-scope and application-scoped reference to one reusable managed policy.
    /// Managed policy definitions are deliberately not tenant-owned.
    /// </summary>
    public sealed record ManagedPolicyReference
    {
        /// <summary>Gets the identity scope identifier.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the policy identifier.</summary>
        public Guid PolicyId { get; }

        /// <summary>Initializes a new instance of <see cref="ManagedPolicyReference"/>.</summary>
        public ManagedPolicyReference(Guid identityScopeId, ApplicationKey application, Guid policyId)
        {
            IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            Application = application;
            PolicyId = ModelGuard.Identifier(policyId, nameof(policyId));
        }
    }
}
