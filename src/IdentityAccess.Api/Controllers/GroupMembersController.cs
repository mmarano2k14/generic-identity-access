using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for group members.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/groups/{groupId:guid}/members")]
    [Produces("application/json")]
    public sealed class GroupMembersController(
        OptionalFeature<IDirectoryAdministrationService> feature,
        OptionalFeature<ITenantGroupAssignmentDelegationGuard> delegationFeature) : ControllerBase
    {
        /// <summary>Lists group members.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.GroupMemberships, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<GroupMemberResponse>>> List(
            [FromRoute] Guid identityScopeId, [FromRoute] Guid tenantId, [FromRoute] string applicationKey,
            [FromRoute] Guid groupId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var members = await service.ListGroupMembersAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), groupId, cancellationToken);
            return Ok(members.Select(GroupMemberResponse.From).ToArray());
        }

        /// <summary>Adds group members.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.GroupMemberships, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<GroupMemberResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupMemberResponse>> Add(
            [FromRoute] Guid identityScopeId, [FromRoute] Guid tenantId, [FromRoute] string applicationKey,
            [FromRoute] Guid groupId, [FromBody] AddGroupMemberRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            if (!delegationFeature.TryGet(out var delegation)) return ApiProblems.AdministrationAuthorizationUnavailable();
            var trustedContext = HttpContext.Features.Get<AdministrationRequestContextFeature>();
            if (trustedContext is null) return ApiProblems.AdministrationAuthorizationUnavailable();

            var application = new ApplicationKey(applicationKey);
            var delegationResult = await delegation.AuthorizeAssignmentAsync(
                trustedContext.Context, tenantId, application, groupId, cancellationToken);
            if (delegationResult.Decision == AdministrationAccessDecision.Denied)
                return ApiProblems.Forbidden(
                    "Group assignment delegation denied",
                    "The selected group would delegate authority outside the caller's permitted tenant scope.");
            if (delegationResult.Decision != AdministrationAccessDecision.Allowed)
                return ApiProblems.AdministrationAuthorizationUnavailable();

            var edge = await service.AddGroupMemberAsync(identityScopeId, tenantId,
                application, groupId, request.TenantMembershipId, cancellationToken);
            return edge is null
                ? ApiProblems.NotFound("Group or tenant membership not found")
                : Created(Request.Path, GroupMemberResponse.From(edge));
        }

        /// <summary>Removes group members.</summary>
        [HttpDelete("{tenantMembershipId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.GroupMemberships, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> Remove(
            [FromRoute] Guid identityScopeId, [FromRoute] Guid tenantId, [FromRoute] string applicationKey,
            [FromRoute] Guid groupId, [FromRoute] Guid tenantMembershipId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var removed = await service.RemoveGroupMemberAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), groupId, tenantMembershipId, cancellationToken);
            return removed ? NoContent() : NotFound();
        }

    }
}
