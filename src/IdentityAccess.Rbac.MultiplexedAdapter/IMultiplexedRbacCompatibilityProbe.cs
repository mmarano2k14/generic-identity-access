namespace IdentityAccess.Rbac.MultiplexedAdapter
{
    /// <summary>
    /// Exposes a neutral compatibility preflight for the configured external RBAC distribution
    /// without leaking external assembly types.
    /// </summary>
    public interface IMultiplexedRbacCompatibilityProbe
    {
        /// <summary>
        /// Validates and pins the external RBAC distribution used by this adapter instance.
        /// </summary>
        MultiplexedRbacCompatibilityReport ProbeCompatibility();
    }
}
