using System.ComponentModel.DataAnnotations;
using IdentityAccess.Api;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;
using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Exposes HTTP endpoints for authentication.</summary>
    [ApiController]
    [Route("api/v1/authentication/clients/{clientId}")]
    [Produces("application/json")]
    [RequireLocalAuthentication]
    public sealed class AuthenticationController(OptionalFeature<ILocalAuthenticationService> feature) : ControllerBase
    {
        /// <summary>Authenticates a user with a password and issues an opaque local session when successful.</summary>
        [HttpPost("password-login")]
        [ProducesResponseType<PasswordLoginResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<PasswordLoginResponse>> PasswordLogin([FromRoute] string clientId,
            [FromBody] PasswordLoginRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.AuthenticationUnavailable();
            if (string.IsNullOrWhiteSpace(request.LoginIdentifier) || string.IsNullOrEmpty(request.Password) ||
                string.IsNullOrWhiteSpace(request.RedirectUri))
                return ApiProblems.BadRequest("Invalid login request");

            var result = await service.LoginAsync(clientId, request.LoginIdentifier, request.Password,
                request.RedirectUri, cancellationToken);
            return result.Decision switch
            {
                PasswordLoginDecision.Succeeded => Ok(new PasswordLoginResponse(
                    result.UserId!.Value, result.SessionId!.Value, result.SessionToken!, result.ExpiresAt!.Value,
                    result.RedirectUri!)),
                PasswordLoginDecision.InvalidCredentials => ApiProblems.Unauthorized(
                    "Authentication failed",
                    "The credentials are invalid."),
                PasswordLoginDecision.ClientRejected => ApiProblems.BadRequest("Unknown authentication client"),
                PasswordLoginDecision.RedirectRejected => ApiProblems.BadRequest("Redirect URI rejected"),
                _ => ApiProblems.AuthenticationUnavailable()
            };
        }

        /// <summary>Validates an opaque local session for the registered authentication client.</summary>
        [HttpPost("sessions/validate")]
        [ProducesResponseType<SessionValidationResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SessionValidationResponse>> ValidateSession([FromRoute] string clientId,
            [FromBody] SessionValidationRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.AuthenticationUnavailable();
            var result = await service.ValidateSessionAsync(clientId, request.SessionId, request.SessionToken,
                cancellationToken);
            return result.Valid && result.Context is not null
                ? Ok(new SessionValidationResponse(
                    result.Context.Subject.UserId,
                    result.Context.SessionId,
                    result.Context.ExpiresAt))
                : ApiProblems.Unauthorized(
                    "Invalid session",
                    "The session is invalid, expired or revoked.");
        }

        /// <summary>Revokes the requested session and validates an optional post-logout redirect URI.</summary>
        [HttpPost("logout")]
        public async Task<ActionResult<LogoutResponse>> Logout([FromRoute] string clientId,
            [FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            if (!feature.TryGet(out var service)) return ApiProblems.AuthenticationUnavailable();
            var result = await service.LogoutAsync(clientId, request.SessionId, request.SessionToken,
                request.PostLogoutRedirectUri, cancellationToken);
            if (result.FailureCode is AuthenticationFailureCode.UnknownClient or AuthenticationFailureCode.PostLogoutRedirectUriRejected)
                return ApiProblems.BadRequest("Logout request rejected");
            if (result.FailureCode == AuthenticationFailureCode.DirectoryUnavailable) return ApiProblems.AuthenticationUnavailable();
            return Ok(new LogoutResponse(result.PostLogoutRedirectUri));
        }

    }
}
