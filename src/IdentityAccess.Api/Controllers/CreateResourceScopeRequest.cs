using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for create resource scope.</summary>
    public sealed record CreateResourceScopeRequest(Guid ResourceScopeId, int ModelVersion, string ScopeType,
        string ExternalResourceId, string DisplayName, Guid? ParentResourceScopeId,
        ResourceScopeStatus Status = ResourceScopeStatus.Active);
}
