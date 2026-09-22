

namespace IdentityAccess.Domain
{

    /// <summary>Defines lifecycle states for tenant.</summary>
    public enum TenantStatus
    {
        /// <summary>Indicates that the entity is active.</summary>
        Active = 1,
        /// <summary>Indicates that the entity is suspended.</summary>
        Suspended = 2
    }
}
