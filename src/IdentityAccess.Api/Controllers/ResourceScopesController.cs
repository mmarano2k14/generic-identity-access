using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for resource scopes.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/resource-scopes")]
    [Produces("application/json")]
    public sealed class ResourceScopesController(OptionalFeature<IResourceScopeAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists resource scopes.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.ResourceScopes, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<ResourceScopeResponse>>> List(Guid identityScopeId, Guid tenantId,
            string applicationKey, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ResourceScopeAdministrationUnavailable();
            var rows = await service.ListAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey), cancellationToken);
            return Ok(rows.Select(ResourceScopeResponse.From).ToArray());
        }

        /// <summary>Gets the requested resource scopes.</summary>
        [HttpGet("{resourceScopeId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.ResourceScopes, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<ResourceScopeResponse>> Get(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid resourceScopeId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ResourceScopeAdministrationUnavailable();
            var row = await service.GetAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey),
                resourceScopeId, cancellationToken);
            return row is null ? NotFound() : Ok(ResourceScopeResponse.From(row));
        }

        /// <summary>Creates resource scopes.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.ResourceScopes, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<ResourceScopeResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ResourceScopeResponse>> Create(Guid identityScopeId, Guid tenantId,
            string applicationKey, [FromBody] CreateResourceScopeRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ResourceScopeAdministrationUnavailable();
            var id = request.ResourceScopeId == Guid.Empty ? Guid.NewGuid() : request.ResourceScopeId;
            var row = await service.CreateAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey), id,
                request.ModelVersion, new ResourceScopeTypeKey(request.ScopeType), request.ExternalResourceId,
                request.DisplayName, request.ParentResourceScopeId, request.Status, cancellationToken);
            return CreatedAtAction(nameof(Get), new { identityScopeId, tenantId, applicationKey, resourceScopeId = id },
                ResourceScopeResponse.From(row));
        }

        /// <summary>Updates resource scopes.</summary>
        [HttpPut("{resourceScopeId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.ResourceScopes, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<ResourceScopeResponse>> Update(Guid identityScopeId, Guid tenantId,
            string applicationKey, Guid resourceScopeId, [FromBody] UpdateResourceScopeRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.ResourceScopeAdministrationUnavailable();
            var row = await service.UpdateAsync(identityScopeId, tenantId, new ApplicationKey(applicationKey),
                resourceScopeId, request.ModelVersion, new ResourceScopeTypeKey(request.ScopeType),
                request.ExternalResourceId, request.DisplayName, request.ParentResourceScopeId, request.Status,
                request.ExpectedVersion, cancellationToken);
            return Ok(ResourceScopeResponse.From(row));
            
        }

    }
}
