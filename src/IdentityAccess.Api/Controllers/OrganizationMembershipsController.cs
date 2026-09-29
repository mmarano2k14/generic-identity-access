using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using Microsoft.AspNetCore.Mvc;
using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers explicit tenant-member belonging inside one organization.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/organizations/{organizationId:guid}/memberships")]
    [Produces("application/json")]
    public sealed class OrganizationMembershipsController(
        OptionalFeature<IOrganizationMembershipAdministrationService> feature,
        IOrganizationSecurityAuditWriter auditWriter) : ControllerBase
    {
        /// <summary>Lists organization memberships.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationMemberships,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganizationMembershipResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
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

            var memberships = await service.ListForOrganizationAsync(
                Organization(identityScopeId, tenantId, organizationId),
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(
                memberships
                    .Select(OrganizationMembershipResponse.From)
                    .ToArray());
        }

        /// <summary>Gets one organization membership.</summary>
        [HttpGet("{tenantMembershipId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationMemberships,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<OrganizationMembershipResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            Guid tenantMembershipId,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationMembershipAdministrationUnavailable();
            }

            var membership = await service.GetAsync(
                Organization(identityScopeId, tenantId, organizationId),
                TenantMembership(identityScopeId, tenantId, tenantMembershipId),
                cancellationToken);

            return membership is null
                ? NotFound()
                : Ok(OrganizationMembershipResponse.From(membership));
        }

        /// <summary>Adds an active tenant member to an active organization.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationMemberships,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<OrganizationMembershipResponse>(
            StatusCodes.Status201Created)]
        public async Task<ActionResult<OrganizationMembershipResponse>> Add(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            [FromBody] CreateOrganizationMembershipRequest request,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (request.TenantMembershipId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationMembershipAdministrationUnavailable();
            }

            var membership = await service.AddAsync(
                Organization(identityScopeId, tenantId, organizationId),
                TenantMembership(
                    identityScopeId,
                    tenantId,
                    request.TenantMembershipId),
                cancellationToken);

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationMembershipAdded,
                $"{organizationId:D}:{request.TenantMembershipId:D}",
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    identityScopeId,
                    applicationKey,
                    tenantId,
                    organizationId,
                    tenantMembershipId = request.TenantMembershipId
                },
                OrganizationMembershipResponse.From(membership));
        }

        /// <summary>Suspends organization belonging while retaining the durable relation.</summary>
        [HttpPost("{tenantMembershipId:guid}/suspend")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationMemberships,
            IdentityAccessAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganizationMembershipResponse>> Suspend(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            Guid tenantMembershipId,
            [FromBody] OrganizationMembershipLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                tenantId,
                organizationId,
                tenantMembershipId,
                request.ExpectedRowVersion,
                OrganizationMembershipStatus.Suspended,
                cancellationToken);

        /// <summary>Reactivates organization belonging after current references are revalidated.</summary>
        [HttpPost("{tenantMembershipId:guid}/activate")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationMemberships,
            IdentityAccessAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganizationMembershipResponse>> Activate(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            Guid tenantMembershipId,
            [FromBody] OrganizationMembershipLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                tenantId,
                organizationId,
                tenantMembershipId,
                request.ExpectedRowVersion,
                OrganizationMembershipStatus.Active,
                cancellationToken);

        /// <summary>Removes organization belonging without deleting the user or tenant membership.</summary>
        [HttpDelete("{tenantMembershipId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.OrganizationMemberships,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> Remove(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            Guid tenantMembershipId,
            [FromQuery] long expectedRowVersion,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (expectedRowVersion <= 0)
            {
                return BadRequest();
            }

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationMembershipAdministrationUnavailable();
            }

            var removed = await service.RemoveAsync(
                Organization(identityScopeId, tenantId, organizationId),
                TenantMembership(identityScopeId, tenantId, tenantMembershipId),
                expectedRowVersion,
                cancellationToken);

            if (!removed)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationMembershipRemoved,
                $"{organizationId:D}:{tenantMembershipId:D}",
                cancellationToken);

            return NoContent();
        }

        private async Task<ActionResult<OrganizationMembershipResponse>> ChangeStatus(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            Guid tenantMembershipId,
            long expectedRowVersion,
            OrganizationMembershipStatus status,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (expectedRowVersion <= 0)
            {
                return BadRequest();
            }

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationMembershipAdministrationUnavailable();
            }

            var updated = await service.SetStatusAsync(
                Organization(identityScopeId, tenantId, organizationId),
                TenantMembership(identityScopeId, tenantId, tenantMembershipId),
                status,
                expectedRowVersion,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationMembershipStatusChanged,
                $"{organizationId:D}:{tenantMembershipId:D}",
                cancellationToken);

            return Ok(OrganizationMembershipResponse.From(updated));
        }

        private static OrganizationReference Organization(
            Guid identityScopeId,
            Guid tenantId,
            Guid organizationId) =>
            new(
                new IdentityScopeId(identityScopeId),
                new TenantId(tenantId),
                new OrganizationId(organizationId));

        private static TenantMembershipReference TenantMembership(
            Guid identityScopeId,
            Guid tenantId,
            Guid tenantMembershipId) =>
            new(
                new IdentityScopeId(identityScopeId),
                new TenantId(tenantId),
                new TenantMembershipId(tenantMembershipId));
    }
}
