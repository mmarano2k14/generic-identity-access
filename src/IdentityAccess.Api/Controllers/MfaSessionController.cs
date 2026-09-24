using IdentityAccess.Api.Features;
using IdentityAccess.Api.Http;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Mfa.Recovery;
using IdentityAccess.Mfa.Totp;
using IdentityAccess.Mfa.WebAuthn;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes authenticated local-session MFA step-up operations.</summary>
    [ApiController]
    [Route("api/v1/authentication/clients/{clientId}/mfa")]
    [Produces("application/json")]
    [RequireLocalAuthentication]
    public sealed class MfaSessionController(
        OptionalFeature<ILocalAuthenticationService> authentication,
        OptionalFeature<IAuthenticationAssuranceService> assurance,
        OptionalFeature<ITotpAuthenticationFactorService> totp,
        OptionalFeature<IRecoveryAuthenticationFactorService> recovery,
        OptionalFeature<IWebAuthnAuthenticationService> webAuthn)
        : ControllerBase
    {
        /// <summary>Verifies a TOTP proof and upgrades the exact authenticated session.</summary>
        [HttpPost("totp/{authenticatorId:guid}/verify")]
        public async Task<ActionResult<AuthenticationAssuranceResponse>> VerifyTotp(
            [FromRoute] string clientId,
            [FromRoute] Guid authenticatorId,
            [FromBody] TotpSessionStepUpRequest request,
            CancellationToken cancellationToken)
        {
            if (!totp.TryGet(out var provider) || !assurance.TryGet(out var assuranceService))
                return ApiProblems.AuthenticationUnavailable();

            var session = await ResolveSessionAsync(clientId, cancellationToken).ConfigureAwait(false);
            if (session is null)
                return InvalidSession();

            if (authenticatorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code))
                return ApiProblems.BadRequest("Invalid TOTP step-up request");

            var verification = await provider.VerifyAsync(
                session.Subject.IdentityScopeId,
                session.Application,
                session.Subject.UserId,
                authenticatorId,
                request.Code,
                cancellationToken).ConfigureAwait(false);

            if (verification != TotpVerificationResult.Succeeded)
                return InvalidFactor();

            return await UpgradeAsync(
                assuranceService,
                session,
                AuthenticationMethodReferences.OneTimePassword,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Consumes one recovery code and upgrades the exact authenticated session.</summary>
        [HttpPost("recovery/{authenticatorId:guid}/verify")]
        public async Task<ActionResult<AuthenticationAssuranceResponse>> VerifyRecovery(
            [FromRoute] string clientId,
            [FromRoute] Guid authenticatorId,
            [FromBody] RecoverySessionStepUpRequest request,
            CancellationToken cancellationToken)
        {
            if (!recovery.TryGet(out var provider) || !assurance.TryGet(out var assuranceService))
                return ApiProblems.AuthenticationUnavailable();

            var session = await ResolveSessionAsync(clientId, cancellationToken).ConfigureAwait(false);
            if (session is null)
                return InvalidSession();

            if (authenticatorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code))
                return ApiProblems.BadRequest("Invalid recovery step-up request");

            var verification = await provider.VerifyAsync(
                session.Subject.IdentityScopeId,
                session.Application,
                session.Subject.UserId,
                authenticatorId,
                request.Code,
                cancellationToken).ConfigureAwait(false);

            if (verification != RecoveryCodeVerificationResult.Succeeded)
                return InvalidFactor();

            return await UpgradeAsync(
                assuranceService,
                session,
                AuthenticationMethodReferences.Recovery,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Begins a WebAuthn assertion ceremony for the exact authenticated session.</summary>
        [HttpPost("webauthn/options")]
        public async Task<ActionResult<WebAuthnAuthenticationOptions>> BeginWebAuthn(
            [FromRoute] string clientId,
            CancellationToken cancellationToken)
        {
            if (!webAuthn.TryGet(out var provider))
                return ApiProblems.AuthenticationUnavailable();

            var session = await ResolveSessionAsync(clientId, cancellationToken).ConfigureAwait(false);
            if (session is null)
                return InvalidSession();

            var options = await provider.BeginAuthenticationAsync(
                session.Subject.IdentityScopeId,
                session.Application,
                session.Subject.UserId,
                cancellationToken).ConfigureAwait(false);

            return Ok(options);
        }

        /// <summary>Completes a WebAuthn assertion and upgrades the exact authenticated session.</summary>
        [HttpPost("webauthn/complete")]
        public async Task<ActionResult<AuthenticationAssuranceResponse>> CompleteWebAuthn(
            [FromRoute] string clientId,
            [FromBody] WebAuthnSessionAuthenticationRequest request,
            CancellationToken cancellationToken)
        {
            if (!webAuthn.TryGet(out var provider) || !assurance.TryGet(out var assuranceService))
                return ApiProblems.AuthenticationUnavailable();

            var session = await ResolveSessionAsync(clientId, cancellationToken).ConfigureAwait(false);
            if (session is null)
                return InvalidSession();

            if (request.ChallengeId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.CredentialId) ||
                string.IsNullOrWhiteSpace(request.ClientDataJson) ||
                string.IsNullOrWhiteSpace(request.AuthenticatorData) ||
                string.IsNullOrWhiteSpace(request.Signature))
            {
                return ApiProblems.BadRequest("Invalid WebAuthn step-up request");
            }

            var verification = await provider.CompleteAuthenticationAsync(
                session.Subject.IdentityScopeId,
                session.Application,
                session.Subject.UserId,
                request.ChallengeId,
                request.ToProviderResponse(),
                cancellationToken).ConfigureAwait(false);

            if (verification != WebAuthnAuthenticationResult.Succeeded)
                return InvalidFactor();

            return await UpgradeAsync(
                assuranceService,
                session,
                AuthenticationMethodReferences.ProofOfPossession,
                cancellationToken).ConfigureAwait(false);
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

        private async Task<ActionResult<AuthenticationAssuranceResponse>> UpgradeAsync(
            IAuthenticationAssuranceService assuranceService,
            AuthenticatedSessionContext session,
            string factorMethodReference,
            CancellationToken cancellationToken)
        {
            var updated = await assuranceService.RecordFactorAsync(
                session,
                factorMethodReference,
                cancellationToken).ConfigureAwait(false);

            return updated is null
                ? InvalidSession()
                : Ok(AuthenticationAssuranceResponse.From(updated.Assurance));
        }

        private ObjectResult InvalidSession() =>
            ApiProblems.Unauthorized(
                "Invalid session",
                "The session is invalid, expired or revoked.");

        private ObjectResult InvalidFactor() =>
            ApiProblems.Unauthorized(
                "MFA verification failed",
                "The authentication-factor proof was rejected.");
    }
}
