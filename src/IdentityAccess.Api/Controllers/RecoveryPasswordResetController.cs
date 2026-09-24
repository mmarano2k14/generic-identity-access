using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Mfa.Recovery;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes recovery-code-backed account recovery without account-existence disclosure.</summary>
    [ApiController]
    [Route("api/v1/authentication/clients/{clientId}/recovery")]
    [Produces("application/json")]
    [RequireLocalAuthentication]
    public sealed class RecoveryPasswordResetController(
        OptionalFeature<IRecoveryPasswordResetService> recovery)
        : ControllerBase
    {
        /// <summary>
        /// Consumes one existing recovery code and replaces the password. All invalid account,
        /// authenticator, and recovery-code states intentionally return the same response.
        /// </summary>
        [HttpPost("password")]
        public async Task<IActionResult> ResetPassword(
            [FromRoute] string clientId,
            [FromBody] RecoveryPasswordResetRequest request,
            CancellationToken cancellationToken)
        {
            if (!recovery.TryGet(out var service))
                return ApiProblems.AuthenticationUnavailable();

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(request.LoginIdentifier) ||
                string.IsNullOrWhiteSpace(request.RecoveryCode) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                request.NewPassword.Length is < 12 or > 256)
            {
                return ApiProblems.BadRequest("Invalid account-recovery request");
            }

            var result = await service.ResetPasswordAsync(
                clientId,
                request.LoginIdentifier,
                request.RecoveryCode,
                request.NewPassword,
                cancellationToken).ConfigureAwait(false);

            return result switch
            {
                RecoveryPasswordResetDecision.Succeeded => NoContent(),
                RecoveryPasswordResetDecision.UnknownClient => ApiProblems.BadRequest("Unknown authentication client"),
                RecoveryPasswordResetDecision.Unavailable => ApiProblems.AuthenticationUnavailable(),
                _ => ApiProblems.Unauthorized(
                    "Account recovery failed",
                    "The supplied account-recovery proof was rejected.")
            };
        }
    }
}
