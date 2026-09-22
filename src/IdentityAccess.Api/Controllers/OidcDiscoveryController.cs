using IdentityAccess.Api.Features;
using IdentityAccess.Api.Oidc;
using IdentityAccess.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Exposes OpenID Provider Configuration and JSON Web Key Set metadata.</summary>
    [ApiController]
    [Produces("application/json")]
    public sealed class OidcDiscoveryController(
        OptionalFeature<IOidcAuthorizationService> oidc)
        : ControllerBase
    {
        /// <summary>Returns the OpenID Provider Configuration document.</summary>
        [HttpGet("/.well-known/openid-configuration")]
        public ActionResult<OidcDiscoveryResponse> Configuration()
        {
            if (!oidc.TryGet(out var service))
            {
                return NotFound();
            }

            var metadata =
                service.Metadata;

            return Ok(
                new OidcDiscoveryResponse(
                    metadata.Issuer,
                    metadata.AuthorizationEndpoint,
                    metadata.TokenEndpoint,
                    metadata.JwksUri,
                    ["code"],
                    ["query"],
                    ["authorization_code", "refresh_token"],
                    ["public"],
                    ["RS256"],
                    ["openid"],
                    ["none"],
                    ["S256"],
                    [
                        "iss",
                        "sub",
                        "aud",
                        "exp",
                        "iat",
                        "auth_time",
                        "nonce",
                        "sid",
                        "at_hash"
                    ]));
        }

        /// <summary>Returns the published RSA public signing-key set.</summary>
        [HttpGet("/.well-known/jwks.json")]
        public ActionResult<OidcJsonWebKeySetResponse> Jwks()
        {
            if (!oidc.TryGet(out var service))
            {
                return NotFound();
            }

            return Ok(
                new OidcJsonWebKeySetResponse(
                    service.SigningKeys
                        .Select(OidcJsonWebKeyResponse.From)
                        .ToArray()));
        }
    }
}
