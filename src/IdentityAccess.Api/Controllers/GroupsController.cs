using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes HTTP endpoints for real tenant groups and reusable group cloning.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/groups")]
    [Produces("application/json")]
    public sealed class GroupsController(
        OptionalFeature<IDirectoryAdministrationService> feature,
        OptionalFeature<IGroupDefinitionMutationGuard> definitionGuard,
        OptionalFeature<ITenantGroupAssignmentDelegationGuard> delegationGuard) : ControllerBase
    {
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<GroupRecordResponse>>> List(Guid identityScopeId, Guid tenantId,
            string applicationKey, [FromQuery] string? search, [FromQuery] int? offset, [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var records = await service.ListGroupsAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey), search,
                resolvedOffset, resolvedLimit, cancellationToken);
            return Ok(records.Select(GroupRecordResponse.From).ToArray());
        }

        /// <summary>Lists active reusable real groups that may be cloned into the current tenant.</summary>
        [HttpGet("templates")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<GroupRecordResponse>>> ListTemplates(Guid identityScopeId, Guid tenantId,
            string applicationKey, [FromQuery] string? search, [FromQuery] int? offset, [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            _ = tenantId;
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var records = await service.ListGroupTemplatesAsync(identityScopeId, new ApplicationKey(applicationKey), search,
                resolvedOffset, resolvedLimit, true, cancellationToken);
            return Ok(records.Select(GroupRecordResponse.From).ToArray());
        }

        /// <summary>Lists source resource scopes that must be mapped before cloning one reusable group.</summary>
        [HttpGet("templates/{sourceTenantId:guid}/{sourceGroupId:guid}/scope-requirements")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<GroupTemplateResourceScopeRequirementResponse>>> ListTemplateScopeRequirements(
            Guid identityScopeId,
            Guid tenantId,
            string applicationKey,
            Guid sourceTenantId,
            Guid sourceGroupId,
            CancellationToken cancellationToken)
        {
            _ = tenantId;
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var requirements = await service.ListGroupTemplateScopeRequirementsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                sourceTenantId,
                sourceGroupId,
                cancellationToken);
            return Ok(requirements.Select(GroupTemplateResourceScopeRequirementResponse.From).ToArray());
        }

        [HttpGet("{groupId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<GroupRecordResponse>> Get(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid groupId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var record = await service.GetGroupAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey),
                groupId, cancellationToken);
            return record is null ? NotFound() : Ok(GroupRecordResponse.From(record));
        }

        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<GroupRecordResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupRecordResponse>> Create(Guid identityScopeId, Guid tenantId,
            string applicationKey, [FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var groupId = request.GroupId == Guid.Empty ? Guid.NewGuid() : request.GroupId;
            var created = await service.CreateGroupAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey),
                groupId, request.DisplayName, request.Status, cancellationToken);
            return CreatedAtAction(nameof(Get), new { identityScopeId, tenantId, applicationKey, groupId },
                GroupRecordResponse.From(created));
        }

        /// <summary>Creates a normal tenant group by cloning a reusable group's definition and managed bindings.</summary>
        [HttpPost("from-template")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<GroupRecordResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupRecordResponse>> CreateFromTemplate(Guid identityScopeId, Guid tenantId,
            string applicationKey, [FromBody] CreateGroupFromTemplateRequest request, CancellationToken cancellationToken)
        {
            if (request.SourceTenantId == Guid.Empty || request.SourceGroupId == Guid.Empty) return BadRequest();

            var mappings = new Dictionary<Guid, Guid>();
            foreach (var mapping in request.ResourceScopeMappings ?? [])
            {
                if (mapping.SourceResourceScopeId == Guid.Empty || mapping.TargetResourceScopeId == Guid.Empty)
                    return ApiProblems.BadRequest(
                        "Invalid resource-scope mapping",
                        "Source and target resource-scope identifiers must be non-empty.");
                if (!mappings.TryAdd(mapping.SourceResourceScopeId, mapping.TargetResourceScopeId))
                    return ApiProblems.BadRequest(
                        "Duplicate resource-scope mapping",
                        $"Source resource scope '{mapping.SourceResourceScopeId:D}' is mapped more than once.");
            }

            if (!delegationGuard.TryGet(out var delegation)) return ApiProblems.AdministrationAuthorizationUnavailable();
            var trustedContext = HttpContext.Features.Get<AdministrationRequestContextFeature>();
            if (trustedContext is null) return ApiProblems.AdministrationAuthorizationUnavailable();
            var application = new ApplicationKey(applicationKey);
            var delegationResult = await delegation.AuthorizeGrantCopyAsync(
                trustedContext.Context,
                request.SourceTenantId,
                tenantId,
                application,
                request.SourceGroupId,
                mappings,
                cancellationToken);
            if (delegationResult.Decision == AdministrationAccessDecision.Denied)
                return ApiProblems.Forbidden(
                    "Group template delegation denied",
                    "The reusable group would copy authority outside the caller's permitted target-tenant scope.");
            if (delegationResult.Decision != AdministrationAccessDecision.Allowed)
                return ApiProblems.AdministrationAuthorizationUnavailable();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();

            var groupId = request.GroupId == Guid.Empty ? Guid.NewGuid() : request.GroupId;
            try
            {
                var created = await service.CreateGroupFromTemplateAsync(
                    identityScopeId,
                    tenantId,
                    application,
                    request.SourceTenantId,
                    request.SourceGroupId,
                    groupId,
                    mappings,
                    cancellationToken);
                return created is null
                    ? NotFound()
                    : StatusCode(StatusCodes.Status201Created, GroupRecordResponse.From(created));
            }
            catch (GroupTemplateScopeMappingException exception)
            {
                return ApiProblems.UnprocessableEntity(
                    "Template resource-scope mapping required",
                    exception.Message);
            }
        }

        [HttpPut("{groupId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<GroupRecordResponse>> Update(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid groupId, [FromBody] UpdateGroupRequest request,
            CancellationToken cancellationToken)
        {
            var application = new ApplicationKey(applicationKey);
            if (!definitionGuard.TryGet(out var guard)) return ApiProblems.AdministrationAuthorizationUnavailable();
            var access = await guard.AuthorizeAsync(HttpContext, identityScopeId, tenantId, application, groupId,
                IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Write, cancellationToken);
            if (access.Decision == AdministrationAccessDecision.Unauthenticated) return Unauthorized();
            if (access.Decision == AdministrationAccessDecision.Denied) return Forbid();
            if (access.Decision != AdministrationAccessDecision.Allowed) return ApiProblems.AdministrationAuthorizationUnavailable();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var updated = await service.UpdateGroupAsync(identityScopeId, tenantId, application, groupId,
                request.DisplayName, request.Status, request.ExpectedVersion, cancellationToken);
            return Ok(GroupRecordResponse.From(updated));
        }

        /// <summary>
        /// Identity-scope-only update for a real group's reusable definition. The route intentionally
        /// has no tenantId route key, so the normal authorization pipeline cannot borrow tenant authority.
        /// </summary>
        [HttpPut("~/api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/reusable-groups/{sourceTenantId:guid}/{groupId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<GroupRecordResponse>> UpdateReusable(Guid identityScopeId,
            string applicationKey, Guid sourceTenantId, Guid groupId, [FromBody] UpdateReusableGroupRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var updated = await service.UpdateReusableGroupAsync(identityScopeId, sourceTenantId,
                new ApplicationKey(applicationKey), groupId, request.DisplayName, request.Status, request.IsTemplate,
                request.ExpectedVersion, cancellationToken);
            return Ok(GroupRecordResponse.From(updated));
        }
    }
}
