using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using Microsoft.AspNetCore.Mvc;
using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers application-aware Organization-to-ResourceScope linkage.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/organizations/{organizationId:guid}/resource-scope-link")]
    [Produces("application/json")]
    public sealed class OrganizationResourceScopeLinksController(
        OptionalFeature<IOrganizationResourceScopeLinkAdministrationService> feature,
        IOrganizationSecurityAuditWriter auditWriter)
        : ControllerBase
    {
        /// <summary>Gets the current application's ResourceScope link.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationScopeLinks,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<OrganizationResourceScopeLinkResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationResourceScopeLinkAdministrationUnavailable();
            }

            var link = await service.GetAsync(
                Organization(identityScopeId, tenantId, organizationId),
                new ApplicationKey(applicationKey),
                cancellationToken);

            return link is null
                ? NotFound()
                : Ok(OrganizationResourceScopeLinkResponse.From(link));
        }

        /// <summary>Links the Organization to one active ResourceScope in the current application.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationScopeLinks,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<OrganizationResourceScopeLinkResponse>(
            StatusCodes.Status201Created)]
        public async Task<ActionResult<OrganizationResourceScopeLinkResponse>> Create(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            [FromBody] CreateOrganizationResourceScopeLinkRequest request,
            CancellationToken cancellationToken)
        {
            if (request.ResourceScopeId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationResourceScopeLinkAdministrationUnavailable();
            }

            var link = await service.LinkAsync(
                Organization(identityScopeId, tenantId, organizationId),
                new ApplicationKey(applicationKey),
                new ResourceScopeId(request.ResourceScopeId),
                cancellationToken);

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationResourceScopeLinked,
                $"{organizationId:D}:{request.ResourceScopeId:D}",
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    identityScopeId,
                    applicationKey,
                    tenantId,
                    organizationId
                },
                OrganizationResourceScopeLinkResponse.From(link));
        }

        /// <summary>Replaces the current application's linked ResourceScope.</summary>
        [HttpPut]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationScopeLinks,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<OrganizationResourceScopeLinkResponse>> Update(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            [FromBody] UpdateOrganizationResourceScopeLinkRequest request,
            CancellationToken cancellationToken)
        {
            if (request.ResourceScopeId == Guid.Empty ||
                request.ExpectedRowVersion <= 0)
            {
                return BadRequest();
            }

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationResourceScopeLinkAdministrationUnavailable();
            }

            var link = await service.RelinkAsync(
                Organization(identityScopeId, tenantId, organizationId),
                new ApplicationKey(applicationKey),
                new ResourceScopeId(request.ResourceScopeId),
                request.ExpectedRowVersion,
                cancellationToken);

            if (link is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationResourceScopeRelinked,
                $"{organizationId:D}:{request.ResourceScopeId:D}",
                cancellationToken);

            return Ok(OrganizationResourceScopeLinkResponse.From(link));
        }

        /// <summary>Removes the current application's ResourceScope link.</summary>
        [HttpDelete]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationScopeLinks,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> Delete(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            [FromQuery] long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            if (expectedRowVersion <= 0)
            {
                return BadRequest();
            }

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationResourceScopeLinkAdministrationUnavailable();
            }

            var deleted = await service.RemoveAsync(
                Organization(identityScopeId, tenantId, organizationId),
                new ApplicationKey(applicationKey),
                expectedRowVersion,
                cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationResourceScopeUnlinked,
                organizationId.ToString("D"),
                cancellationToken);

            return NoContent();
        }

        private static OrganizationReference Organization(
            Guid identityScopeId,
            Guid tenantId,
            Guid organizationId) =>
            new(
                new IdentityScopeId(identityScopeId),
                new TenantId(tenantId),
                new OrganizationId(organizationId));
    }
}
