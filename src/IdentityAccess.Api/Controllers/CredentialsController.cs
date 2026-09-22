using System.ComponentModel.DataAnnotations;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Security;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for credentials.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/users/{userId:guid}/password-credential")]
    [Produces("application/json")]
    public sealed class CredentialsController(OptionalFeature<ICredentialAdministrationService> feature) : ControllerBase
    {
        /// <summary>Gets the requested credentials.</summary>
        [HttpGet]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Credentials, IdentityAccessAdministrationCapabilities.Read)]
        public async Task<ActionResult<CredentialMetadataResponse>> Get([FromRoute] Guid identityScopeId,
            [FromRoute] string applicationKey, [FromRoute] Guid userId, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.CredentialAdministrationUnavailable();
            var record = await service.GetAsync(identityScopeId, new ApplicationKey(applicationKey), userId,
                cancellationToken);
            return record is null ? NotFound() : Ok(CredentialMetadataResponse.From(record));
        }

        /// <summary>Creates credentials.</summary>
        [HttpPost]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Credentials, IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<CredentialMetadataResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult<CredentialMetadataResponse>> Create([FromRoute] Guid identityScopeId,
            [FromRoute] string applicationKey, [FromRoute] Guid userId, [FromBody] CreatePasswordCredentialRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.CredentialAdministrationUnavailable();
            var created = await service.CreateAsync(identityScopeId, new ApplicationKey(applicationKey), userId,
                request.LoginIdentifier, request.Password, cancellationToken);
            return Created(Request.Path, CredentialMetadataResponse.From(created));
        }

        /// <summary>Changes the password credential for the requested user.</summary>
        [HttpPut]
        [RequireAdministrationCapability(IdentityAccessAdministrationCapabilities.Resource, IdentityAccessAdministrationCapabilities.Credentials, IdentityAccessAdministrationCapabilities.Write)]
        public async Task<ActionResult<CredentialMetadataResponse>> ChangePassword([FromRoute] Guid identityScopeId,
            [FromRoute] string applicationKey, [FromRoute] Guid userId, [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.CredentialAdministrationUnavailable();
            var updated = await service.ChangePasswordAsync(identityScopeId, new ApplicationKey(applicationKey),
                userId, request.LoginIdentifier, request.Password, request.ExpectedVersion, cancellationToken);
            return Ok(CredentialMetadataResponse.From(updated));
            
        }

    }
}
