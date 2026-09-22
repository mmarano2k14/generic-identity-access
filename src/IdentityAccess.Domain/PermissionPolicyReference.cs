

namespace IdentityAccess.Domain
{

    /// <summary>A tenant- and application-scoped permission policy identity.</summary>
    public sealed record PermissionPolicyReference
    {
        /// <summary>Gets the tenant.</summary>
        public TenantReference Tenant { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the policy identifier.</summary>
        public Guid PolicyId { get; }

        /// <summary>Initializes a new instance of <see cref="PermissionPolicyReference"/>.</summary>
        public PermissionPolicyReference(TenantReference tenant, ApplicationKey application, Guid policyId)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(application);
            Tenant = tenant;
            Application = application;
            PolicyId = ModelGuard.Identifier(policyId, nameof(policyId));
        }
    }
}
