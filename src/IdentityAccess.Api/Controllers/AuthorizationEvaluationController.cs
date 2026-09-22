using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>
    /// Exposes authenticated capability evaluation for external application runtimes such as
    /// TypeScript/Next.js without duplicating RBAC logic outside the .NET authorization boundary.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public sealed class AuthorizationEvaluationController(
        IAdministrationRequestAuthorizer authorizer)
        : ControllerBase
    {
        /// <summary>Evaluates an identity-scope capability.</summary>
        [HttpPost("api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/authorization/evaluate")]
        [ProducesResponseType<AuthorizationEvaluationResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public Task<ActionResult<AuthorizationEvaluationResponse>> EvaluateIdentityScope(
            [FromBody] AuthorizationEvaluationRequest request,
            CancellationToken cancellationToken) =>
            EvaluateAsync(
                request,
                cancellationToken);

        /// <summary>Evaluates a tenant-scoped capability.</summary>
        [HttpPost("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/authorization/evaluate")]
        [ProducesResponseType<AuthorizationEvaluationResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public Task<ActionResult<AuthorizationEvaluationResponse>> EvaluateTenant(
            [FromBody] AuthorizationEvaluationRequest request,
            CancellationToken cancellationToken) =>
            EvaluateAsync(
                request,
                cancellationToken);

        /// <summary>Evaluates a resource-scope capability.</summary>
        [HttpPost("api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/resource-scopes/{resourceScopeId:guid}/authorization/evaluate")]
        [ProducesResponseType<AuthorizationEvaluationResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public Task<ActionResult<AuthorizationEvaluationResponse>> EvaluateResourceScope(
            [FromBody] AuthorizationEvaluationRequest request,
            CancellationToken cancellationToken) =>
            EvaluateAsync(
                request,
                cancellationToken);

        private async Task<ActionResult<AuthorizationEvaluationResponse>> EvaluateAsync(
            AuthorizationEvaluationRequest request,
            CancellationToken cancellationToken)
        {
            CapabilityKey capability;

            try
            {
                capability = new CapabilityKey(
                    request.Resource ?? string.Empty,
                    request.Feature ?? string.Empty,
                    request.Action ?? string.Empty);
            }
            catch (ArgumentException)
            {
                return ApiProblems.BadRequest(
                    "Capability request rejected");
            }

            var result = await authorizer
                .AuthorizeAsync(
                    HttpContext,
                    capability.Resource,
                    capability.Feature,
                    capability.Action,
                    cancellationToken)
                .ConfigureAwait(false);

            return result.Decision switch
            {
                AdministrationAccessDecision.Allowed =>
                    Ok(new AuthorizationEvaluationResponse(true)),

                AdministrationAccessDecision.Denied
                    when result.FailureCode is null =>
                    Ok(new AuthorizationEvaluationResponse(false)),

                AdministrationAccessDecision.Denied =>
                    ApiProblems.Forbidden(
                        "Authorization boundary rejected"),

                AdministrationAccessDecision.Unauthenticated =>
                    ApiProblems.Unauthorized(
                        "Authentication required"),

                _ =>
                    ApiProblems.AdministrationAuthorizationUnavailable()
            };
        }
    }
}
