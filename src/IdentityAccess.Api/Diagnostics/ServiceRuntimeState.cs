namespace IdentityAccess.Api.Diagnostics
{
    /// <summary>
    /// Represents one immutable runtime capability snapshot used to derive diagnostics and
    /// readiness.
    /// </summary>
    internal sealed record ServiceRuntimeState(
        bool DatabaseRoutingConfigured,
        bool StorageConfigured,
        bool AuthenticationConfigured,
        bool AuthorizationConfigured,
        bool Ready,
        IReadOnlyList<string> BlockingCapabilities);
}
