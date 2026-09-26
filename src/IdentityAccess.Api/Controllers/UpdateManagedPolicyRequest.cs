using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Updates managed policy metadata without changing version contents.</summary>
    public sealed record UpdateManagedPolicyRequest(
        string PolicyKey,
        string DisplayName,
        PolicyStatus Status,
        long ExpectedVersion);
}
