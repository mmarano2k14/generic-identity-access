namespace IdentityAccess.Api.Controllers
{
    /// <summary>Publishes and freezes one managed-policy version.</summary>
    public sealed record PublishManagedPolicyVersionRequest(bool MakeDefault = false);
}
