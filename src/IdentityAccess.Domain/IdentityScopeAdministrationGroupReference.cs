namespace IdentityAccess.Domain
{
    /// <summary>An identity-scope administration group scoped to one application.</summary>
    public sealed record IdentityScopeAdministrationGroupReference
    {
        /// <summary>Gets the identity scope.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the group identifier.</summary>
        public Guid GroupId { get; }

        /// <summary>Initializes a scope-administration group reference.</summary>
        public IdentityScopeAdministrationGroupReference(Guid identityScopeId, ApplicationKey application, Guid groupId)
        {
            IdentityScopeId = ModelGuard.Identifier(identityScopeId, nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            Application = application;
            GroupId = ModelGuard.Identifier(groupId, nameof(groupId));
        }
    }
}
