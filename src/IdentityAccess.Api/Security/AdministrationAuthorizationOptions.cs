using IdentityAccess.Rbac;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Defines the trusted server-side RBAC execution context used by administration authorization.
    /// These values are configuration, not caller-provided request data.
    /// </summary>
    internal sealed record AdministrationAuthorizationOptions
    {
        /// <summary>Gets the canonical external RBAC project.</summary>
        public string RbacProject { get; }

        /// <summary>Gets the canonical external RBAC namespace.</summary>
        public string RbacNamespace { get; }

        /// <summary>Initializes validated administration authorization options.</summary>
        public AdministrationAuthorizationOptions(
            string rbacProject,
            string rbacNamespace)
        {
            RbacProject = new RbacContextKey(rbacProject).Value;
            RbacNamespace = new RbacContextKey(rbacNamespace).Value;
        }
    }
}
