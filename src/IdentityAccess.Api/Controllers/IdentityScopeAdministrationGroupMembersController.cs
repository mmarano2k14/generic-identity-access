using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers identity-scope authority group memberships.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/scope-authority/groups/{groupId:guid}/members")]
    [Produces("application/json")]
    public sealed class IdentityScopeAdministrationGroupMembersController(
        OptionalFeature<IIdentityScopeAuthorityAdministrationService> feature)
        : ControllerBase
    {
        /// <summary>Lists members.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityMemberships,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<IdentityScopeAdministrationMemberResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var members = await service.ListMembersAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                cancellationToken);

            return Ok(members.Select(IdentityScopeAdministrationMemberResponse.From).ToArray());
        }

        /// <summary>Adds an active user to an active authority group.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityMemberships,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<IdentityScopeAdministrationMemberResponse>> Add(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            [FromBody] AddIdentityScopeAdministrationMemberRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var membership = await service.AddMemberAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                request.UserId,
                cancellationToken);

            return membership is null
                ? ApiProblems.NotFound("Active authority group or active user not found")
                : Created(Request.Path, IdentityScopeAdministrationMemberResponse.From(membership));
        }

        /// <summary>Removes a member.</summary>
        [HttpDelete("{userId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityMemberships,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> Remove(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var removed = await service.RemoveMemberAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                userId,
                cancellationToken);

            return removed ? NoContent() : NotFound();
        }
    }
}
