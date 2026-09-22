using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Administers identity-scope authority group-policy bindings.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/scope-authority/groups/{groupId:guid}/policy-bindings")]
    [Produces("application/json")]
    public sealed class IdentityScopeAdministrationPolicyBindingsController(
        OptionalFeature<IIdentityScopeAuthorityAdministrationService> feature)
        : ControllerBase
    {
        /// <summary>Lists bindings.</summary>
        [HttpGet]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityBindings,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<IdentityScopeAdministrationPolicyBindingResponse>>> List(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var bindings = await service.ListBindingsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                cancellationToken);

            return Ok(bindings.Select(IdentityScopeAdministrationPolicyBindingResponse.From).ToArray());
        }

        /// <summary>Adds a binding when both group and policy are active.</summary>
        [HttpPost]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityBindings,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<IdentityScopeAdministrationPolicyBindingResponse>> Add(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            [FromBody] AddIdentityScopeAdministrationPolicyBindingRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var binding = await service.AddBindingAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                request.PolicyId,
                cancellationToken);

            return binding is null
                ? ApiProblems.NotFound("Active authority group or active policy not found")
                : Created(Request.Path, IdentityScopeAdministrationPolicyBindingResponse.From(binding));
        }

        /// <summary>Removes a binding.</summary>
        [HttpDelete("{policyId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityBindings,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> Remove(
            Guid identityScopeId,
            string applicationKey,
            Guid groupId,
            Guid policyId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.IdentityScopeAuthorityAdministrationUnavailable();

            var removed = await service.RemoveBindingAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                groupId,
                policyId,
                cancellationToken);

            return removed ? NoContent() : NotFound();
        }
    }
}
