using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes the server-trusted effective administration context for the current subject.</summary>
    [ApiController]
    [Route("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/administration-context")]
    [Produces("application/json")]
    public sealed class AdministrationContextController(
        OptionalFeature<IEffectiveAdministrationContextService> feature,
        IAdministrationRequestContextResolver contextResolver) : ControllerBase
    {
        /// <summary>Returns safe subject and tenant-visibility metadata after trusted authentication.</summary>
        [HttpGet]
        public async Task<ActionResult<EffectiveAdministrationContextResponse>> Get(
            Guid identityScopeId,
            string applicationKey,
            CancellationToken cancellationToken)
        {
            var authentication = await contextResolver.ResolveAsync(HttpContext, cancellationToken).ConfigureAwait(false);

            if (authentication.Decision == AdministrationAuthenticationDecision.Unavailable)
                return ApiProblems.AdministrationAuthorizationUnavailable();

            if (authentication.Decision != AdministrationAuthenticationDecision.Authenticated ||
                authentication.Context is null)
            {
                return ApiProblems.Unauthorized(
                    "Authentication required",
                    "A valid administration session is required for this operation.");
            }

            var trustedContext = authentication.Context;
            if (!AdministrationRequestBoundary.Matches(HttpContext, trustedContext))
            {
                return ApiProblems.Forbidden(
                    "Forbidden",
                    "The requested administration boundary does not match the authenticated context.");
            }

            if (!feature.TryGet(out var service))
                return ApiProblems.DirectoryAdministrationUnavailable();

            AdministrationAuditActivityContext.Apply(trustedContext);
            HttpContext.Features.Set(new AdministrationRequestContextFeature(trustedContext));

            var effective = await service.ResolveAsync(
                identityScopeId,
                new ApplicationKey(applicationKey),
                trustedContext.Subject,
                cancellationToken).ConfigureAwait(false);

            return Ok(EffectiveAdministrationContextResponse.From(effective));
        }
    }
}
