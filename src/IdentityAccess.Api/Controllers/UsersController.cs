using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for users.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/users")]
    [Produces("application/json")]
    public sealed class UsersController(OptionalFeature<IDirectoryAdministrationService> feature) : ControllerBase
    {
        /// <summary>Lists users in a bounded deterministic window.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Users, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<IReadOnlyList<UserRecordResponse>>> List(Guid identityScopeId,
            string applicationKey, [FromQuery] string? search, [FromQuery] int? offset, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var resolvedOffset = offset ?? 0;
            var resolvedLimit = limit ?? AdministrationPaging.DefaultLimit;
            if (!AdministrationPaging.IsValid(resolvedOffset, resolvedLimit)) return BadRequest();
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var records = await service.ListUsersAsync(identityScopeId, new ApplicationKey(applicationKey), search, resolvedOffset,
                resolvedLimit, cancellationToken);
            return Ok(records.Select(UserRecordResponse.From).ToArray());
        }

        /// <summary>Gets the requested users.</summary>
        [HttpGet("{userId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Users, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<UserRecordResponse>> Get(Guid identityScopeId, string applicationKey,
            Guid userId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var record = await service.GetUserAsync(identityScopeId, new ApplicationKey(applicationKey), userId,
                cancellationToken);
            return record is null ? NotFound() : Ok(UserRecordResponse.From(record));
        }

        /// <summary>Creates users.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Users, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<UserRecordResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<UserRecordResponse>> Create(Guid identityScopeId, string applicationKey,
            [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var userId = request.UserId == Guid.Empty ? Guid.NewGuid() : request.UserId;
            var created = await service.CreateUserAsync(identityScopeId, new ApplicationKey(applicationKey), userId,
                request.DisplayName, request.Status, cancellationToken);
            return CreatedAtAction(nameof(Get), new { identityScopeId, applicationKey, userId },
                UserRecordResponse.From(created));
        }

        /// <summary>Updates users.</summary>
        [HttpPut("{userId:guid}")]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Users, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<UserRecordResponse>> Update(Guid identityScopeId, string applicationKey,
            Guid userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.DirectoryAdministrationUnavailable();
            var updated = await service.UpdateUserAsync(identityScopeId, new ApplicationKey(applicationKey), userId,
                request.DisplayName, request.Status, request.ExpectedVersion, cancellationToken);
            return Ok(UserRecordResponse.From(updated));
            
        }

    }
}
