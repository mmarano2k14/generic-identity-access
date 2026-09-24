using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes authenticated self-service credential operations.</summary>
    [ApiController]
    [Route("api/v1/authentication/clients/{clientId}/credentials")]
    [Produces("application/json")]
    [RequireLocalAuthentication]
    public sealed class SelfServiceCredentialsController(
        OptionalFeature<ILocalAuthenticationService> authentication,
        OptionalFeature<ISelfServiceCredentialService> credentials)
        : ControllerBase
    {
        /// <summary>
        /// Re-authenticates the current subject, enforces recent MFA when required, replaces the
        /// password, and revokes every existing session for the subject.
        /// </summary>
        [HttpPost("password/change")]
        public async Task<IActionResult> ChangePassword(
            [FromRoute] string clientId,
            [FromBody] SelfServicePasswordChangeRequest request,
            CancellationToken cancellationToken)
        {
            if (!credentials.TryGet(out var service))
                return ApiProblems.AuthenticationUnavailable();

            if (string.IsNullOrEmpty(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                request.NewPassword.Length is < 12 or > 256)
            {
                return ApiProblems.BadRequest("Invalid password-change request");
            }

            var session = await ResolveSessionAsync(clientId, cancellationToken).ConfigureAwait(false);
            if (session is null)
                return InvalidSession();

            var result = await service.ChangePasswordAsync(
                session,
                request.CurrentPassword,
                request.NewPassword,
                cancellationToken).ConfigureAwait(false);

            return result switch
            {
                SelfServicePasswordChangeDecision.Succeeded => NoContent(),
                SelfServicePasswordChangeDecision.InvalidSession => InvalidSession(),
                SelfServicePasswordChangeDecision.InvalidCurrentPassword => ApiProblems.Unauthorized(
                    "Password change rejected",
                    "The current authentication proof was rejected."),
                SelfServicePasswordChangeDecision.RecentMfaRequired => ApiProblems.Forbidden(
                    "Recent MFA required",
                    "Complete a recent MFA step-up before changing the password."),
                SelfServicePasswordChangeDecision.PasswordReuseRejected => ApiProblems.BadRequest(
                    "Password change rejected",
                    "The replacement password must differ from the current password."),
                SelfServicePasswordChangeDecision.ConcurrencyConflict => ApiProblems.Result(
                    StatusCodes.Status409Conflict,
                    "Password changed concurrently",
                    "Reload the credential state and retry the operation."),
                _ => ApiProblems.AuthenticationUnavailable()
            };
        }

        private async Task<AuthenticatedSessionContext?> ResolveSessionAsync(
            string clientId,
            CancellationToken cancellationToken)
        {
            if (!authentication.TryGet(out var service) ||
                string.IsNullOrWhiteSpace(clientId) ||
                !TryReadSessionCredential(out var sessionId, out var sessionToken))
            {
                return null;
            }

            var result = await service.ValidateSessionAsync(
                clientId,
                sessionId,
                sessionToken,
                cancellationToken).ConfigureAwait(false);

            return result.Valid
                ? result.Context
                : null;
        }

        private bool TryReadSessionCredential(
            out Guid sessionId,
            out string sessionToken)
        {
            sessionId = Guid.Empty;
            sessionToken = string.Empty;

            var sessionValues = Request.Headers[LocalSessionAdministrationRequestContextResolver.SessionHeaderName];
            var authorizationValues = Request.Headers["Authorization"];

            if (sessionValues.Count != 1 ||
                authorizationValues.Count != 1 ||
                !Guid.TryParseExact(sessionValues[0], "D", out sessionId))
            {
                return false;
            }

            var authorization = authorizationValues[0];
            const string prefix = LocalSessionAdministrationRequestContextResolver.AuthorizationScheme + " ";

            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith(prefix, StringComparison.Ordinal) ||
                authorization.Length == prefix.Length)
            {
                return false;
            }

            sessionToken = authorization[prefix.Length..];
            return !string.IsNullOrWhiteSpace(sessionToken) && !sessionToken.Any(char.IsWhiteSpace);
        }

        private ObjectResult InvalidSession() =>
            ApiProblems.Unauthorized(
                "Invalid session",
                "The session is invalid, expired or revoked.");
    }
}
