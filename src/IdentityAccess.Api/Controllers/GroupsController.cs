using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for groups.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/groups")]
    [Produces("application/json")]
    public sealed class GroupsController(OptionalFeature<IDirectoryAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists groups in a bounded deterministic window.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<GroupRecordResponse>>> List(Guid identityScopeId, Guid tenantId,
            string applicationKey, [FromQuery] int? offset, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var records = await service.ListGroupsAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey),
                resolvedOffset, resolvedLimit, cancellationToken);
            return Ok(records.Select(GroupRecordResponse.From).ToArray());
        }

        /// <summary>Gets the requested groups.</summary>
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

        /// <summary>Creates groups.</summary>
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

        /// <summary>Updates groups.</summary>
        [HttpPut("{groupId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Groups, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<GroupRecordResponse>> Update(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid groupId, [FromBody] UpdateGroupRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var updated = await service.UpdateGroupAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), groupId, request.DisplayName, request.Status,
                request.ExpectedVersion, cancellationToken);
            return Ok(GroupRecordResponse.From(updated));
            
        }

    }
}
