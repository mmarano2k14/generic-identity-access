using IdentityAccess.Api.Features;
using IdentityAccess.Api.Oidc;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes the OAuth 2.0 token endpoint for registered public clients.</summary>
    [ApiController]
    [Produces("application/json")]
    public sealed class OidcTokenController(
        OptionalFeature<IOidcAuthorizationService> oidc)
        : ControllerBase
    {
        private static readonly string[] TokenParameterNames =
        [
            "client_id",
            "grant_type",
            "code",
            "redirect_uri",
            "code_verifier",
            "refresh_token"
        ];

        /// <summary>Consumes an authorization code or rotates an opaque refresh token.</summary>
        [HttpPost("/connect/token")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType<OidcTokenResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<OidcErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<OidcErrorResponse>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<OidcTokenResponse>> Token(
            [FromForm(Name = "client_id")] string? clientId,
            [FromForm(Name = "grant_type")] string? grantType,
            [FromForm(Name = "code")] string? code,
            [FromForm(Name = "redirect_uri")] string? redirectUri,
            [FromForm(Name = "code_verifier")] string? codeVerifier,
            [FromForm(Name = "refresh_token")] string? refreshToken,
            CancellationToken cancellationToken)
        {
            if (!oidc.TryGet(out var service))
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new OidcErrorResponse(
                        "temporarily_unavailable"));
            }

            if (await OidcRequestParameterGuard
                .HasDuplicateFormParametersAsync(
                    Request,
                    TokenParameterNames,
                    cancellationToken)
                .ConfigureAwait(false) ||
                await OidcRequestParameterGuard
                    .HasUnexpectedFormParametersAsync(
                        Request,
                        TokenParameterNames,
                        cancellationToken)
                    .ConfigureAwait(false))
            {
                return BadRequest(
                    new OidcErrorResponse(
                        "invalid_request"));
            }

            OidcTokenResult result;

            if (string.Equals(
                    grantType,
                    "refresh_token",
                    StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(code) ||
                    !string.IsNullOrEmpty(redirectUri) ||
                    !string.IsNullOrEmpty(codeVerifier))
                {
                    return BadRequest(
                        new OidcErrorResponse(
                            "invalid_request"));
                }

                result =
                    await service
                        .RefreshAsync(
                            new OidcRefreshTokenRequest(
                                clientId,
                                refreshToken),
                            cancellationToken)
                        .ConfigureAwait(false);
            }
            else
            {
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(
                        new OidcErrorResponse(
                            "invalid_request"));
                }

                result =
                    await service
                        .ExchangeCodeAsync(
                            new OidcTokenRequest(
                                clientId,
                                grantType,
                                code,
                                redirectUri,
                                codeVerifier),
                            cancellationToken)
                        .ConfigureAwait(false);
            }

            if (result.Succeeded)
            {
                return Ok(
                    new OidcTokenResponse(
                        result.AccessToken!,
                        "Bearer",
                        result.ExpiresIn,
                        result.IdToken,
                        result.RefreshToken!,
                        result.Scope!));
            }

            var error =
                result.FailureCode switch
                {
                    OidcTokenFailureCode.InvalidClient =>
                        "invalid_client",

                    OidcTokenFailureCode.InvalidGrant =>
                        "invalid_grant",

                    OidcTokenFailureCode.UnsupportedGrantType =>
                        "unsupported_grant_type",

                    OidcTokenFailureCode.DirectoryUnavailable or
                        OidcTokenFailureCode.TokenIssuanceFailed =>
                        "temporarily_unavailable",

                    _ =>
                        "invalid_request"
                };

            var status =
                error == "temporarily_unavailable"
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status400BadRequest;

            return StatusCode(
                status,
                new OidcErrorResponse(
                    error));
        }
    }
}
