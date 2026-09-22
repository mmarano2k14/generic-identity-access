using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents an identity-scope administration group-policy binding.</summary>
    public sealed record IdentityScopeAdministrationPolicyBindingResponse(Guid GroupId, Guid PolicyId)
    {
        /// <summary>Maps a group-policy binding.</summary>
        public static IdentityScopeAdministrationPolicyBindingResponse From(
            IdentityScopeAdministrationGroupPolicyBinding binding) =>
            new(binding.Group.GroupId, binding.Policy.PolicyId);
    }
}
