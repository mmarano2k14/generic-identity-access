using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Creates one reusable managed policy definition.</summary>
    public sealed record CreateManagedPolicyRequest(
        Guid PolicyId,
        string PolicyKey,
        string DisplayName,
        PolicyStatus Status = PolicyStatus.Active);
}
