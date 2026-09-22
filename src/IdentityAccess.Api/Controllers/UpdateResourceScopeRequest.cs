using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the request payload for update resource scope.</summary>
    public sealed record UpdateResourceScopeRequest(int ModelVersion, string ScopeType, string ExternalResourceId,
        string DisplayName, Guid? ParentResourceScopeId, ResourceScopeStatus Status, long ExpectedVersion);
}
