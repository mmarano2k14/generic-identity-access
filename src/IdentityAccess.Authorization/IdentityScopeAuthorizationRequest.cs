using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Authorization
{
    /// <summary>
    /// Server-side authorization request for an identity-scope administration operation.
    /// </summary>
    public sealed record IdentityScopeAuthorizationRequest
    {
        /// <summary>Gets the identity scope.</summary>
        public Guid IdentityScopeId { get; }

        /// <summary>Gets the authenticated subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the canonical external RBAC project.</summary>
        public string RbacProject { get; }

        /// <summary>Gets the canonical external RBAC namespace.</summary>
        public string RbacNamespace { get; }

        /// <summary>Gets the requested concrete capability.</summary>
        public CapabilityKey Capability { get; }

        /// <summary>Initializes an identity-scope authorization request.</summary>
        public IdentityScopeAuthorizationRequest(
            Guid identityScopeId,
            SubjectReference subject,
            ApplicationKey application,
            string rbacProject,
            string rbacNamespace,
            CapabilityKey capability)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("An identity scope is required.", nameof(identityScopeId));

            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(capability);

            if (subject.IdentityScopeId != identityScopeId)
            {
                throw new ArgumentException(
                    "The subject must belong to the requested identity scope.",
                    nameof(subject));
            }

            IdentityScopeId = identityScopeId;
            Subject = subject;
            Application = application;
            RbacProject = new RbacContextKey(rbacProject).Value;
            RbacNamespace = new RbacContextKey(rbacNamespace).Value;
            Capability = capability;
        }
    }
}
