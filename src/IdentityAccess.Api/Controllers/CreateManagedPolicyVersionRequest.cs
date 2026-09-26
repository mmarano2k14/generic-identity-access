namespace IdentityAccess.Api.Controllers
{
    /// <summary>Creates one draft managed-policy version pinned to a security-model version.</summary>
    public sealed record CreateManagedPolicyVersionRequest(int PolicyVersion, int ModelVersion);
}
