using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using Microsoft.AspNetCore.Mvc;
using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Reads organization belonging from one tenant member's perspective.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/tenant-memberships/{tenantMembershipId:guid}/organizations")]
    [Produces("application/json")]
    public sealed class TenantMembershipOrganizationsController(
        OptionalFeature<IOrganizationMembershipAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists explicit organization memberships for one tenant member.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationMemberships,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganizationMembershipResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid tenantMembershipId,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? 100;

            if (resolvedOffset < 0 || resolvedLimit is < 1 or > 500)
            {
                return BadRequest();
            }

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationMembershipAdministrationUnavailable();
            }

            var memberships = await service.ListForTenantMembershipAsync(
                new TenantMembershipReference(
                    new IdentityScopeId(identityScopeId),
                    new TenantId(tenantId),
                    new TenantMembershipId(tenantMembershipId)),
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(
                memberships
                    .Select(OrganizationMembershipResponse.From)
                    .ToArray());
        }
    }
}
