using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>One immutable-or-draft managed-policy version.</summary>
    public sealed record ManagedPolicyVersionResponse(
        Guid PolicyId,
        int PolicyVersion,
        int ModelVersion,
        DateTimeOffset? PublishedAt)
    {
        public static ManagedPolicyVersionResponse From(ManagedPolicyVersion version) =>
            new(
                version.Reference.Policy.PolicyId,
                version.Reference.Version,
                version.Model.Version,
                version.PublishedAt);
    }
}
