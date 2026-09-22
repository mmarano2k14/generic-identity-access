using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers identity-scope authority groups.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/scope-authority/groups")]
    [Produces("application/json")]
    public sealed class IdentityScopeAdministrationGroupsController(
        OptionalFeature<IIdentityScopeAuthorityAdministrationService> feature)
        : ControllerBase
    {
        /// <summary>Gets a scope-authority group.</summary>
        [HttpGet("{groupId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityGroups,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IdentityScopeAdministrationGroupResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var record = await service.GetGroupAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                cancellationToken);

            return record is null
                ? NotFound()
                : Ok(IdentityScopeAdministrationGroupResponse.From(record));
        }

        /// <summary>Creates a scope-authority group.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityGroups,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<IdentityScopeAdministrationGroupResponse>> Create(
            Guid identityScopeId,
            string applicationKey,
            [FromBody] CreateGroupRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var groupId = request.GroupId == Guid.Empty ? Guid.NewGuid() : request.GroupId;

            var created = await service.CreateGroupAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                request.DisplayName,
                request.Status,
                cancellationToken);

            return CreatedAtAction(
                nameof(Get),
                new { identityScopeId, applicationKey, groupId },
                IdentityScopeAdministrationGroupResponse.From(created));
        }

        /// <summary>Updates a scope-authority group.</summary>
        [HttpPut("{groupId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityGroups,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<IdentityScopeAdministrationGroupResponse>> Update(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            [FromBody] UpdateGroupRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var updated = await service.UpdateGroupAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                request.DisplayName,
                request.Status,
                request.ExpectedVersion,
                cancellationToken);

            return Ok(IdentityScopeAdministrationGroupResponse.From(updated));
        }
    }
}
