using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for group policy binding.</summary>
    public sealed record GroupPolicyBindingResponse(Guid GroupId, Guid PolicyId, Guid? ResourceScopeId,
        bool IncludeDescendants)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static GroupPolicyBindingResponse From(GroupPolicyBinding binding) =>
            new(binding.Group.GroupId, binding.Policy.PolicyId, binding.TargetScope?.ResourceScopeId,
                binding.IncludeDescendants);
    }
}
