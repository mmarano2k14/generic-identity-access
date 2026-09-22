using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for scope types.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/security-models/{modelVersion:int}/scope-types")]
    [Produces("application/json")]
    public sealed class ScopeTypesController(OptionalFeature<IResourceScopeAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists scope types.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.ScopeTypes, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ScopeTypeResponse>>> List(Guid identityScopeId,
            string applicationKey, int modelVersion, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ResourceScopeAdministrationUnavailable();
            var rows = await service.ListTypesAsync(identityScopeId, new ApplicationKey(applicationKey),
                modelVersion, cancellationToken);
            return Ok(rows.Select(ScopeTypeResponse.From).ToArray());
        }

        /// <summary>Adds scope types.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.ScopeTypes, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<ScopeTypeResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ScopeTypeResponse>> Add(Guid identityScopeId, string applicationKey,
            int modelVersion, [FromBody] AddScopeTypeRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ResourceScopeAdministrationUnavailable();
            var definition = await service.AddTypeAsync(identityScopeId, new ApplicationKey(applicationKey), modelVersion,
                new ResourceScopeTypeKey(request.Key), request.DisplayName,
                request.ParentKey is null ? null : new ResourceScopeTypeKey(request.ParentKey),
                request.CanAttachToTenant, cancellationToken);
            return Created(Request.Path, ScopeTypeResponse.From(definition));
        }

    }
}
