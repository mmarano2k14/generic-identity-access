using IdentityAccess.Api.Features;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>
    /// Resolves an existing validated local session for the OIDC authorization endpoint. The raw
    /// session token remains transport-only and is never persisted by this resolver.
    /// </summary>
    internal sealed class OidcLocalSessionResolver(
        OptionalFeature<ILocalAuthenticationService> authentication)
        : IOidcLocalSessionResolver
    {
        /// <summary>Resolves a validated local session for the requested registered client.</summary>
        public async ValueTask<AuthenticatedSessionContext?> ResolveAsync(
            HttpContext httpContext,
            string? clientId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            if (string.IsNullOrWhiteSpace(clientId) ||
                !authentication.TryGet(out var service))
            {
                return null;
            }

            var sessionValues =
                httpContext.Request.Headers[
                    LocalSessionAdministrationRequestContextResolver.SessionHeaderName];

            var authorizationValues =
                httpContext.Request.Headers["Authorization"];

            if (sessionValues.Count != 1 ||
                authorizationValues.Count != 1 ||
                !Guid.TryParseExact(
                    sessionValues[0],
                    "D",
                    out var sessionId))
            {
                return null;
            }

            var authorization =
                authorizationValues[0];

            const string prefix =
                LocalSessionAdministrationRequestContextResolver.AuthorizationScheme + " ";

            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith(
                    prefix,
                    StringComparison.Ordinal) ||
                authorization.Length == prefix.Length)
            {
                return null;
            }

            var sessionToken =
                authorization[prefix.Length..];

            if (string.IsNullOrWhiteSpace(sessionToken) ||
                sessionToken.Any(char.IsWhiteSpace))
            {
                return null;
            }

            var result =
                await service
                    .ValidateSessionAsync(
                        clientId,
                        sessionId,
                        sessionToken,
                        cancellationToken)
                    .ConfigureAwait(false);

            return result.Valid
                ? result.Context
                : null;
        }
    }
}
