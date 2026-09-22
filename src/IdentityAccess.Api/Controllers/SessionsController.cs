using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes administrative bulk revocation of local authentication sessions.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/sessions")]
    [Produces("application/json")]
    public sealed class SessionsController(
        OptionalFeature<ISessionAdministrationService> feature) : ControllerBase
    {
        /// <summary>Revokes every active session for the requested user.</summary>
        [HttpDelete("users/{userId:guid}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Sessions,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<SessionRevocationResponse>(StatusCodes.Status200OK)]
        public async Task<ActionResult<SessionRevocationResponse>> RevokeUserSessions(
            [FromRoute] Guid identityScopeId,
            [FromRoute] string applicationKey,
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.AuthenticationUnavailable();

            var count = await service.RevokeUserSessionsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                userId,
                cancellationToken);

            return Ok(new SessionRevocationResponse(count));
        }

        /// <summary>Revokes every active session for the requested registered client.</summary>
        [HttpDelete("clients/{clientId}")]
        [RequireAdministrationCapability(
            IdentityAccessAdministrationCapabilities.Resource,
            IdentityAccessAdministrationCapabilities.Sessions,
            IdentityAccessAdministrationCapabilities.Write)]
        [ProducesResponseType<SessionRevocationResponse>(StatusCodes.Status200OK)]
        public async Task<ActionResult<SessionRevocationResponse>> RevokeClientSessions(
            [FromRoute] Guid identityScopeId,
            [FromRoute] string applicationKey,
            [FromRoute] string clientId,
            CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service))
                return ApiProblems.AuthenticationUnavailable();

            var count = await service.RevokeClientSessionsAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                clientId,
                cancellationToken);

            return Ok(new SessionRevocationResponse(count));
        }
    }
}
