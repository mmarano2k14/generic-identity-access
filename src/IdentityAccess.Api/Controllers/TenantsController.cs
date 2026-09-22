using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for tenants.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants")]
    [Produces("application/json")]
    public sealed class TenantsController(OptionalFeature<IDirectoryAdministrationService> feature) : ControllerBase
    {
        /// <summary>Gets the requested tenants.</summary>
        [HttpGet("{tenantId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Tenants, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<TenantRecordResponse>> Get(Guid identityScopeId, string applicationKey,
            Guid tenantId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var record = await service.GetTenantAsync(identityScopeId, new ApplicationKey(applicationKey), tenantId,
                cancellationToken);
            return record is null ? NotFound() : Ok(TenantRecordResponse.From(record));
        }

        /// <summary>Creates tenants.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Tenants, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<TenantRecordResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<TenantRecordResponse>> Create(Guid identityScopeId, string applicationKey,
            [FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var tenantId = request.TenantId == Guid.Empty ? Guid.NewGuid() : request.TenantId;
            var created = await service.CreateTenantAsync(identityScopeId, new ApplicationKey(applicationKey), tenantId,
                request.DisplayName, request.Status, cancellationToken);
            return CreatedAtAction(nameof(Get), new { identityScopeId, applicationKey, tenantId },
                TenantRecordResponse.From(created));
        }

        /// <summary>Updates tenants.</summary>
        [HttpPut("{tenantId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Tenants, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<TenantRecordResponse>> Update(Guid identityScopeId, string applicationKey,
            Guid tenantId, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var updated = await service.UpdateTenantAsync(identityScopeId, new ApplicationKey(applicationKey),
                tenantId, request.DisplayName, request.Status, request.ExpectedVersion, cancellationToken);
            return Ok(TenantRecordResponse.From(updated));
            
        }

    }
}
