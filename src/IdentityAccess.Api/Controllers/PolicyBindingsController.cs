using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for policy bindings.</summary>
    [NonController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/groups/{groupId:guid}/policy-bindings")]
    [Produces("application/json")]
    public sealed class PolicyBindingsController(OptionalFeature<IPolicyAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists policy bindings.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.PolicyBindings, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<GroupPolicyBindingResponse>>> List(Guid identityScopeId,
            Guid tenantId, string applicationKey, Guid groupId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var bindings = await service.ListBindingsAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), groupId, cancellationToken);
            return Ok(bindings.Select(GroupPolicyBindingResponse.From).ToArray());
        }

        /// <summary>Adds policy bindings.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.PolicyBindings, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<GroupPolicyBindingResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupPolicyBindingResponse>> Add(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid groupId, [FromBody] AddGroupPolicyBindingRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var binding = await service.AddBindingAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), groupId, request.PolicyId, request.ResourceScopeId,
                request.IncludeDescendants, cancellationToken);
            return binding is null ? NotFound() : Created(Request.Path, GroupPolicyBindingResponse.From(binding));
        }

        /// <summary>Removes policy bindings.</summary>
        [HttpDelete("{policyId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.PolicyBindings, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> Remove(Guid identityScopeId, Guid tenantId, string applicationKey,
            Guid groupId, Guid policyId, [FromQuery] Guid? resourceScopeId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.PolicyAdministrationUnavailable();
            var removed = await service.RemoveBindingAsync(identityScopeId, tenantId,
                new ApplicationKey(applicationKey), groupId, policyId, resourceScopeId, cancellationToken);
            return removed ? NoContent() : NotFound();
        }

    }
}
