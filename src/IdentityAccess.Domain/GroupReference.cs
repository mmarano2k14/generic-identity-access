

namespace IdentityAccess.Domain
{

    /// <summary>A user group scoped to an application and tenant; it is not an external application group identifier.</summary>
    public sealed record GroupReference
    {
        /// <summary>Gets the tenant.</summary>
        public TenantReference Tenant { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the group identifier.</summary>
        public Guid GroupId { get; }

        /// <summary>Initializes a new instance of <see cref="GroupReference"/>.</summary>
        public GroupReference(TenantReference tenant, ApplicationKey application, Guid groupId)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(application);
            Tenant = tenant;
            Application = application;
            GroupId = ModelGuard.Identifier(groupId, nameof(groupId));
        }
    }
}
