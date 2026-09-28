using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exact-login lookup for controlled tenant-membership addition.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/tenants/{tenantId:guid}/membership-candidates")]
    [Produces("application/json")]
    public sealed class TenantMembershipCandidatesController(
        OptionalFeature<ITenantMembershipCandidateService> feature,
        OptionalFeature<IDirectoryAdministrationService> directoryFeature) : ControllerBase
    {
        [HttpGet("by-login")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.TenantMemberships, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<TenantMembershipCandidateResponse>> FindByLogin(
            Guid identityScopeId, string applicationKey, Guid tenantId, [FromQuery] string loginIdentifier,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(loginIdentifier) || loginIdentifier.Trim().Length > LoginIdentifier.MaximumLength) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var candidate = await service.FindByLoginAsync(identityScopeId, new ApplicationKey(applicationKey),
                tenantId, loginIdentifier, cancellationToken);
            return candidate is null ? NotFound() : Ok(TenantMembershipCandidateResponse.From(candidate));
        }

        [HttpPost("by-login/membership")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.TenantMemberships, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<TenantMembershipRecordResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<TenantMembershipRecordResponse>> CreateMembershipByLogin(
            Guid identityScopeId, string applicationKey, Guid tenantId,
            [FromBody] CreateTenantMembershipByLoginRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.LoginIdentifier) ||
                request.LoginIdentifier.Trim().Length > LoginIdentifier.MaximumLength)
                return BadRequest();
            if (!feature.TryGet(out var candidates) || !directoryFeature.TryGet(out var directory))
                return ApiProblems.DirectoryAdministrationUnavailable();

            var application = new ApplicationKey(applicationKey);
            var candidate = await candidates.FindByLoginAsync(
                identityScopeId, application, tenantId, request.LoginIdentifier, cancellationToken);
            if (candidate is null) return NotFound();
            if (candidate.UserStatus != UserStatus.Active)
                return ApiProblems.BadRequest("Membership candidate is inactive");
            if (candidate.ExistingMembershipId is not null)
                return ApiProblems.Result(StatusCodes.Status409Conflict,
                    "Membership already exists",
                    "The exact account already has a membership in this tenant.");

            var membershipId = request.MembershipId == Guid.Empty ? Guid.NewGuid() : request.MembershipId;
            var created = await directory.CreateTenantMembershipAsync(
                identityScopeId, application, tenantId, membershipId, candidate.UserId, request.Status, cancellationToken);
            return Created(Request.Path, TenantMembershipRecordResponse.From(created));
        }
    }
}
