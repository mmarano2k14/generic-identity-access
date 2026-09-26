using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Tenant-scoped binding to a shared managed-policy version.</summary>
    public sealed record ManagedGroupPolicyBindingResponse(
        Guid GroupId,
        Guid PolicyId,
        int PolicyVersion,
        Guid? ResourceScopeId,
        bool IncludeDescendants)
    {
        public static ManagedGroupPolicyBindingResponse From(ManagedGroupPolicyBinding binding) =>
            new(
                binding.Group.GroupId,
                binding.PolicyVersion.Policy.PolicyId,
                binding.PolicyVersion.Version,
                binding.TargetScope?.ResourceScopeId,
                binding.IncludeDescendants);
    }
}
