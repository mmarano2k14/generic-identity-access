using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Authorization
{
    /// <summary>
    /// Internal server-side authorization request. ResourceScope is optional: null means tenant-level authorization.
    /// Construction validates shape but does not authenticate the caller or prove resource ownership.
    /// </summary>
    public sealed record IdentityAuthorizationRequest
    {
        /// <summary>Gets the tenant.</summary>
        public TenantReference Tenant { get; }

        /// <summary>Gets the subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the canonical RBAC project.</summary>
        public string RbacProject { get; }

        /// <summary>Gets the canonical RBAC namespace.</summary>
        public string RbacNamespace { get; }

        /// <summary>Gets the requested concrete capability.</summary>
        public CapabilityKey Capability { get; }

        /// <summary>Gets the optional resource scope.</summary>
        public ResourceScopeReference? ResourceScope { get; }

        /// <summary>Initializes a new tenant-level authorization request.</summary>
        public IdentityAuthorizationRequest(
            TenantReference tenant,
            SubjectReference subject,
            ApplicationKey application,
            string rbacProject,
            string rbacNamespace,
            CapabilityKey capability)
            : this(tenant, subject, application, rbacProject, rbacNamespace, capability, null)
        {
        }

        /// <summary>Initializes a new authorization request with an optional resource scope.</summary>
        public IdentityAuthorizationRequest(
            TenantReference tenant,
            SubjectReference subject,
            ApplicationKey application,
            string rbacProject,
            string rbacNamespace,
            CapabilityKey capability,
            ResourceScopeReference? resourceScope)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(capability);

            if (tenant.IdentityScopeId != subject.IdentityScopeId)
            {
                throw new ArgumentException("Tenant and subject must belong to the same identity scope.");
            }

            if (resourceScope is not null &&
                (resourceScope.Tenant != tenant || resourceScope.Application != application))
            {
                throw new ArgumentException(
                    "The resource scope must belong to the same tenant and application.",
                    nameof(resourceScope));
            }

            Tenant = tenant;
            Subject = subject;
            Application = application;
            RbacProject = new RbacContextKey(rbacProject).Value;
            RbacNamespace = new RbacContextKey(rbacNamespace).Value;
            Capability = capability;
            ResourceScope = resourceScope;
        }
    }
}
