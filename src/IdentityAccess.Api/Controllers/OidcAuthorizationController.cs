using IdentityAccess.Api.Features;
using IdentityAccess.Api.Oidc;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes the OAuth 2.0 / OpenID Connect authorization endpoint.</summary>
    [ApiController]
    [Produces("application/json")]
    public sealed class OidcAuthorizationController(
        OptionalFeature<IOidcAuthorizationService> oidc,
        IOidcLocalSessionResolver sessionResolver)
        : ControllerBase
    {
        /// <summary>
        /// Validates an Authorization Code + PKCE request against an already authenticated local
        /// session and redirects with either a one-time code or a protocol error.
        /// </summary>
        [HttpGet("/connect/authorize")]
        public async Task<IActionResult> Authorize(
            [FromQuery(Name = "client_id")] string? clientId,
            [FromQuery(Name = "redirect_uri")] string? redirectUri,
            [FromQuery(Name = "response_type")] string? responseType,
            [FromQuery(Name = "scope")] string? scope,
            [FromQuery(Name = "state")] string? state,
            [FromQuery(Name = "nonce")] string? nonce,
            [FromQuery(Name = "code_challenge")] string? codeChallenge,
            [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod,
            CancellationToken cancellationToken)
        {
            if (!oidc.TryGet(out var service))
            {
                return NotFound();
            }

            if (OidcRequestParameterGuard.HasDuplicateQueryParameters(
                    Request,
                    [
                        "client_id",
                        "redirect_uri",
                        "response_type",
                        "scope",
                        "state",
                        "nonce",
                        "code_challenge",
                        "code_challenge_method"
                    ]))
            {
                return BadRequest();
            }

            var session =
                await sessionResolver
                    .ResolveAsync(
                        HttpContext,
                        clientId,
                        cancellationToken)
                    .ConfigureAwait(false);

            var result =
                await service
                    .AuthorizeAsync(
                        new OidcAuthorizationRequest(
                            clientId,
                            redirectUri,
                            responseType,
                            scope,
                            state,
                            nonce,
                            codeChallenge,
                            codeChallengeMethod),
                        session,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (result.Succeeded)
            {
                return Redirect(
                    OidcRedirectBuilder.Success(
                        result));
            }

            if (!string.IsNullOrWhiteSpace(
                    result.RedirectUri))
            {
                return Redirect(
                    OidcRedirectBuilder.Error(
                        result));
            }

            return BadRequest();
        }
    }
}
