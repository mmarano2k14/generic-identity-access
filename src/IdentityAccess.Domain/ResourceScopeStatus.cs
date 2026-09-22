

namespace IdentityAccess.Domain
{

    /// <summary>Defines lifecycle states for resource scope.</summary>
    public enum ResourceScopeStatus : short
    {
        /// <summary>Indicates that the entity is active.</summary>
        Active = 1,
        /// <summary>Indicates that the entity is disabled.</summary>
        Disabled = 2
    }
}
