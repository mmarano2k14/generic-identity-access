using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Tenant-scoped aggregate read surface for group assignments.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/group-memberships")]
    [Produces("application/json")]
    public sealed class TenantGroupAssignmentsController(OptionalFeature<IDirectoryAdministrationService> feature) : ControllerBase
    {
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.GroupMemberships, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<TenantGroupAssignmentResponse>>> List(
            Guid identityScopeId, Guid tenantId, string applicationKey, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var records = await service.ListTenantGroupAssignmentsAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), cancellationToken);
            return Ok(records.Select(TenantGroupAssignmentResponse.From).ToArray());
        }
    }
}
