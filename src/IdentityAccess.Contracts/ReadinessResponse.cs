

namespace IdentityAccess.Contracts
{

    /// <summary>Represents the response payload for readiness.</summary>
    public sealed record ReadinessResponse(bool Ready, string Stage, IReadOnlyList<string> BlockingCapabilities);
}
