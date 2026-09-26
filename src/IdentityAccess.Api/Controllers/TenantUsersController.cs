using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes tenant-constrained user reads without leaking the identity-scope directory.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/users")]
    [Produces("application/json")]
    public sealed class TenantUsersController(OptionalFeature<ITenantUserAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists users joined through the requested tenant before filtering and paging.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Users,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<TenantUserRecordResponse>>> List(
            Guid identityScopeId,
            Guid tenantId,
            string applicationKey,
            [FromQuery] string? search,
            [FromQuery] bool? activeMembershipsOnly,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();

            var records = await service.ListAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                tenantId,
                search,
                activeMembershipsOnly ?? false,
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(records.Select(TenantUserRecordResponse.From).ToArray());
        }
    }
}
