using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Updates an MFA policy using optimistic concurrency.</summary>
    public sealed record UpdateMfaPolicyRequest(
        MfaPolicyMode Mode,
        IReadOnlyList<string> AllowedProviders,
        long ExpectedVersion);
}
