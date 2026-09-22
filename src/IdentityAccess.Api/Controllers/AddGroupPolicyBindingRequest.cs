using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for add group policy binding.</summary>
    public sealed record AddGroupPolicyBindingRequest(Guid PolicyId, Guid? ResourceScopeId = null,
        bool IncludeDescendants = false);
}
