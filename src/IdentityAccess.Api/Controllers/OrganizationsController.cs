using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using Microsoft.AspNetCore.Mvc;
using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers tenant-owned organizations and their hierarchy.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/organizations")]
    [Produces("application/json")]
    public sealed class OrganizationsController(
        OptionalFeature<IOrganizationAdministrationService> feature,
        IOrganizationSecurityAuditWriter auditWriter) : ControllerBase
    {
        /// <summary>Lists organizations in a bounded deterministic window.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganizationResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
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
                return ApiProblems.OrganizationAdministrationUnavailable();
            }

            var organizations = await service.ListAsync(
                new IdentityScopeId(identityScopeId),
                new TenantId(tenantId),
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(organizations.Select(OrganizationResponse.From).ToArray());
        }

        /// <summary>Gets one organization.</summary>
        [HttpGet("{organizationId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<OrganizationResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationAdministrationUnavailable();
            }

            var organization = await service.GetAsync(
                Reference(identityScopeId, tenantId, organizationId),
                cancellationToken);

            return organization is null
                ? NotFound()
                : Ok(OrganizationResponse.From(organization));
        }

        /// <summary>Gets the complete bounded organization hierarchy for one tenant.</summary>
        [HttpGet("tree")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganizationTreeNodeResponse>>> Tree(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationAdministrationUnavailable();
            }

            var roots = await service.GetTreeAsync(
                new IdentityScopeId(identityScopeId),
                new TenantId(tenantId),
                cancellationToken);

            return Ok(roots.Select(OrganizationTreeNodeResponse.From).ToArray());
        }

        /// <summary>Lists direct child organizations.</summary>
        [HttpGet("{organizationId:guid}/children")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<OrganizationResponse>>> Children(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationAdministrationUnavailable();
            }

            var reference = Reference(identityScopeId, tenantId, organizationId);
            var organization = await service.GetAsync(reference, cancellationToken);

            if (organization is null)
            {
                return NotFound();
            }

            var children = await service.ListChildrenAsync(reference, cancellationToken);
            return Ok(children.Select(OrganizationResponse.From).ToArray());
        }

        /// <summary>Creates an active organization.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<OrganizationResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<OrganizationResponse>> Create(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            [FromBody] CreateOrganizationRequest request,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationAdministrationUnavailable();
            }

            var organizationId =
                request.OrganizationId is { } suppliedOrganizationId &&
                suppliedOrganizationId != Guid.Empty
                    ? suppliedOrganizationId
                    : Guid.NewGuid();

            var created = await service.CreateAsync(
                Reference(identityScopeId, tenantId, organizationId),
                new OrganizationKey(request.OrganizationKey),
                request.DisplayName,
                new OrganizationType(request.OrganizationType),
                NormalizeOptionalId(request.ParentOrganizationId),
                cancellationToken);

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationCreated,
                organizationId.ToString("D"),
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
                OrganizationResponse.From(created));
        }

        /// <summary>Updates mutable organization definition state.</summary>
        [HttpPut("{organizationId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<OrganizationResponse>> Update(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            [FromBody] UpdateOrganizationRequest request,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationAdministrationUnavailable();
            }

            var updated = await service.UpdateAsync(
                Reference(identityScopeId, tenantId, organizationId),
                request.DisplayName,
                new OrganizationType(request.OrganizationType),
                NormalizeOptionalId(request.ParentOrganizationId),
                request.ExpectedRowVersion,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            await auditWriter.TryWriteAsync(
                identityScopeId,
                tenantId,
                applicationKey,
                SecurityAuditEventType.OrganizationUpdated,
                organizationId.ToString("D"),
                cancellationToken);

            return Ok(OrganizationResponse.From(updated));
        }

        /// <summary>Disables an organization without deleting durable identity.</summary>
        [HttpPost("{organizationId:guid}/disable")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganizationResponse>> Disable(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            [FromBody] OrganizationLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                tenantId,
                organizationId,
                request.ExpectedRowVersion,
                OrganizationStatus.Disabled,
                cancellationToken);

        /// <summary>Enables a previously disabled organization.</summary>
        [HttpPost("{organizationId:guid}/enable")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Organizations,
            IdentityAccessAdministrationCapabilities.Write)]
        public Task<ActionResult<OrganizationResponse>> Enable(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            [FromBody] OrganizationLifecycleRequest request,
            CancellationToken cancellationToken) =>
            ChangeStatus(
                identityScopeId,
                applicationKey,
                tenantId,
                organizationId,
                request.ExpectedRowVersion,
                OrganizationStatus.Active,
                cancellationToken);

        private async Task<ActionResult<OrganizationResponse>> ChangeStatus(
            Guid identityScopeId,
            string applicationKey,
            Guid tenantId,
            Guid organizationId,
            long expectedRowVersion,
            OrganizationStatus status,
            CancellationToken cancellationToken)
        {
            _ = applicationKey;

            if (!feature.TryGet(out var service))
            {
                return ApiProblems.OrganizationAdministrationUnavailable();
            }

            var updated = await service.SetStatusAsync(
                Reference(identityScopeId, tenantId, organizationId),
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
                SecurityAuditEventType.OrganizationStatusChanged,
                organizationId.ToString("D"),
                cancellationToken);

            return Ok(OrganizationResponse.From(updated));
        }

        private static OrganizationReference Reference(
            Guid identityScopeId,
            Guid tenantId,
            Guid organizationId) =>
            new(
                new IdentityScopeId(identityScopeId),
                new TenantId(tenantId),
                new OrganizationId(organizationId));

        private static OrganizationId? NormalizeOptionalId(Guid? value) =>
            value is null || value == Guid.Empty
                ? null
                : new OrganizationId(value.Value);
    }
}
