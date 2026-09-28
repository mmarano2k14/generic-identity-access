using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>
    /// Administers tenant-scoped bindings to shared managed-policy versions.
    /// </summary>
    [ApiController]
    [Route(
        "api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/managed-policy-bindings")]
    [Produces("application/json")]
    public sealed class ManagedPolicyBindingsController(
        OptionalFeature<IManagedPolicyBindingAdministrationService> feature,
        OptionalFeature<IGroupDefinitionMutationGuard> definitionGuard)
        : ControllerBase
    {
        [HttpGet("available-policies")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.PolicyBindings,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ManagedPolicyResponse>>> ListAvailablePolicies(
            Guid identityScopeId,
            Guid tenantId,
            string applicationKey,
            [FromQuery] string? search,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;

            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit))
                return BadRequest();

            if (!feature.TryGet(out var service))
                return ApiProblems.ManagedPolicyBindingAdministrationUnavailable();

            var policies = await service.ListAvailablePoliciesAsync(
                identityScopeId,
                tenantId,
                new ApplicationKey(applicationKey),
                search,
                resolvedOffset,
                resolvedLimit,
                cancellationToken);

            return Ok(
                policies
                    .Select(ManagedPolicyResponse.From)
                    .ToArray());
        }

        [HttpGet("groups/{groupId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.PolicyBindings,
            IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ManagedGroupPolicyBindingResponse>>> List(
            Guid identityScopeId,
            Guid tenantId,
            string applicationKey,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.ManagedPolicyBindingAdministrationUnavailable();

            var bindings = await service.ListBindingsAsync(
                identityScopeId,
                tenantId,
                new ApplicationKey(applicationKey),
                groupId,
                cancellationToken);

            return Ok(
                bindings
                    .Select(ManagedGroupPolicyBindingResponse.From)
                    .ToArray());
        }

        [HttpPost("groups/{groupId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.PolicyBindings,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<ManagedGroupPolicyBindingResponse>(
            StatusCodes.Status201Created)]
        public async Task<ActionResult<ManagedGroupPolicyBindingResponse>> Add(
            Guid identityScopeId,
            Guid tenantId,
            string applicationKey,
            Guid groupId,
            [FromBody] AddManagedGroupPolicyBindingRequest request,
            CancellationToken cancellationToken)
        {
            if (request.PolicyVersion <= 0)
                return BadRequest();

            if (request.ResourceScopeId is null && request.IncludeDescendants)
                return BadRequest();

            var application = new ApplicationKey(applicationKey);

            var guardResult = await AuthorizeDefinitionMutationAsync(
                identityScopeId,
                tenantId,
                application,
                groupId,
                cancellationToken);

            if (guardResult is not null)
                return guardResult;

            if (!feature.TryGet(out var service))
                return ApiProblems.ManagedPolicyBindingAdministrationUnavailable();

            var binding = await service.AddBindingAsync(
                identityScopeId,
                tenantId,
                application,
                groupId,
                request.PolicyId,
                request.PolicyVersion,
                request.ResourceScopeId,
                request.IncludeDescendants,
                cancellationToken);

            if (binding is null)
                return NotFound();

            return Created(
                Request.Path,
                ManagedGroupPolicyBindingResponse.From(binding));
        }

        [HttpDelete(
            "groups/{groupId:guid}/{policyId:guid}/versions/{policyVersion:int}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.PolicyBindings,
            IdentityAccessAdministrationCapabilities.Write)]
        public async Task<IActionResult> Remove(
            Guid identityScopeId,
            Guid tenantId,
            string applicationKey,
            Guid groupId,
            Guid policyId,
            int policyVersion,
            [FromQuery] Guid? resourceScopeId,
            CancellationToken cancellationToken)
        {
            var application = new ApplicationKey(applicationKey);

            var guardResult = await AuthorizeDefinitionMutationAsync(
                identityScopeId,
                tenantId,
                application,
                groupId,
                cancellationToken);

            if (guardResult is not null)
                return guardResult;

            if (!feature.TryGet(out var service))
                return ApiProblems.ManagedPolicyBindingAdministrationUnavailable();

            var removed = await service.RemoveBindingAsync(
                identityScopeId,
                tenantId,
                application,
                groupId,
                policyId,
                policyVersion,
                resourceScopeId,
                cancellationToken);

            return removed
                ? NoContent()
                : NotFound();
        }

        private async Task<ActionResult?> AuthorizeDefinitionMutationAsync(
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            if (!definitionGuard.TryGet(out var guard))
                return ApiProblems.AdministrationAuthorizationUnavailable();

            var access = await guard.AuthorizeAsync(
                HttpContext,
                identityScopeId,
                tenantId,
                application,
                groupId,
                IdentityAccessAdministrationCapabilities.PolicyBindings,
                IdentityAccessAdministrationCapabilities.Write,
                cancellationToken);

            return access.Decision switch
            {
                AdministrationAccessDecision.Allowed =>
                    null,

                AdministrationAccessDecision.Unauthenticated =>
                    Unauthorized(),

                AdministrationAccessDecision.Denied =>
                    Forbid(),

                _ =>
                    ApiProblems.AdministrationAuthorizationUnavailable()
            };
        }
    }
}
