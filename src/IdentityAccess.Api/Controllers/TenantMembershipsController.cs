using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for tenant memberships.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/memberships")]
    [Produces("application/json")]
    public sealed class TenantMembershipsController(OptionalFeature<IDirectoryAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists tenant memberships in a bounded deterministic window.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.TenantMemberships, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<TenantMembershipRecordResponse>>> List(Guid identityScopeId,
            string applicationKey, Guid tenantId, [FromQuery] string? search, [FromQuery] int? offset, [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var records = await service.ListTenantMembershipsAsync(identityScopeId, new ApplicationKey(applicationKey),
                tenantId, search, resolvedOffset, resolvedLimit, cancellationToken);
            return Ok(records.Select(TenantMembershipRecordResponse.From).ToArray());
        }

        /// <summary>Gets the requested tenant memberships.</summary>
        [HttpGet("{membershipId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.TenantMemberships, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<TenantMembershipRecordResponse>> Get(Guid identityScopeId, string applicationKey,
            Guid tenantId, Guid membershipId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var record = await service.GetTenantMembershipAsync(identityScopeId, new ApplicationKey(applicationKey),
                tenantId, membershipId, cancellationToken);
            return record is null ? NotFound() : Ok(TenantMembershipRecordResponse.From(record));
        }

        /// <summary>Finds the requested by user.</summary>
        [HttpGet("by-user/{userId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.TenantMemberships, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<TenantMembershipRecordResponse>> FindByUser(Guid identityScopeId,
            string applicationKey, Guid tenantId, Guid userId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var record = await service.FindTenantMembershipByUserAsync(identityScopeId,
                new ApplicationKey(applicationKey), tenantId, userId, cancellationToken);
            return record is null ? NotFound() : Ok(TenantMembershipRecordResponse.From(record));
        }

        /// <summary>Creates tenant memberships.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.TenantMemberships, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<TenantMembershipRecordResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<TenantMembershipRecordResponse>> Create(Guid identityScopeId,
            string applicationKey, Guid tenantId, [FromBody] CreateTenantMembershipRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var membershipId = request.MembershipId == Guid.Empty ? Guid.NewGuid() : request.MembershipId;
            var created = await service.CreateTenantMembershipAsync(identityScopeId, new ApplicationKey(applicationKey),
                tenantId, membershipId, request.UserId, request.Status, cancellationToken);
            return CreatedAtAction(nameof(Get), new { identityScopeId, applicationKey, tenantId, membershipId },
                TenantMembershipRecordResponse.From(created));
        }

        /// <summary>Updates tenant memberships.</summary>
        [HttpPut("{membershipId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.TenantMemberships, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<TenantMembershipRecordResponse>> Update(Guid identityScopeId,
            string applicationKey, Guid tenantId, Guid membershipId, [FromBody] UpdateTenantMembershipRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var updated = await service.UpdateTenantMembershipAsync(identityScopeId,
                new ApplicationKey(applicationKey), tenantId, membershipId, request.Status, request.ExpectedVersion,
                cancellationToken);
            return updated is null ? NotFound() : Ok(TenantMembershipRecordResponse.From(updated));
            
        }

    }
}
