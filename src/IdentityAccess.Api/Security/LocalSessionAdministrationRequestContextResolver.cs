using IdentityAccess.Api.Features;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Resolves administration identity from a validated local session. The resolver accepts only
    /// server-registered client/session provenance and never trusts caller-supplied user,
    /// application, tenant, or identity-scope claims.
    /// </summary>
    internal sealed class LocalSessionAdministrationRequestContextResolver(
        OptionalFeature<ILocalAuthenticationService> authentication)
        : IAdministrationRequestContextResolver
    {
        internal const string ClientHeaderName = "X-Identity-Access-Client";
        internal const string SessionHeaderName = "X-Identity-Access-Session";
        internal const string AuthorizationScheme = "IdentitySession";

        /// <inheritdoc />
        public async ValueTask<AdministrationAuthenticationResult> ResolveAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            if (!authentication.TryGet(out var service))
            {
                return AdministrationAuthenticationResult.Unavailable(
                    AdministrationAuthenticationFailureCode.AuthenticationUnavailable);
            }

            if (!TryReadSingleHeader(
                    httpContext,
                    ClientHeaderName,
                    out var clientId) ||
                !TryReadSingleHeader(
                    httpContext,
                    SessionHeaderName,
                    out var sessionValue) ||
                !TryReadSingleHeader(
                    httpContext,
                    "Authorization",
                    out var authorizationValue))
            {
                return AdministrationAuthenticationResult.Unauthenticated(
                    AdministrationAuthenticationFailureCode.CredentialsMissing);
            }

            if (!Guid.TryParseExact(
                    sessionValue,
                    "D",
                    out var sessionId) ||
                !TryReadSessionToken(
                    authorizationValue,
                    out var sessionToken))
            {
                return AdministrationAuthenticationResult.Unauthenticated(
                    AdministrationAuthenticationFailureCode.CredentialsMalformed);
            }

            var validation = await service.ValidateSessionAsync(
                clientId,
                sessionId,
                sessionToken,
                cancellationToken).ConfigureAwait(false);

            if (!validation.Valid ||
                validation.Context is null)
            {
                return AdministrationAuthenticationResult.Unauthenticated(
                    AdministrationAuthenticationFailureCode.SessionInvalid);
            }

            var session = validation.Context;

            return AdministrationAuthenticationResult.Authenticated(
                new AdministrationRequestContext(
                    session.Subject,
                    session.SessionId,
                    session.ClientId,
                    session.Application,
                    session.AuthenticationContextKey,
                    session.ExpiresAt));
        }

        private static bool TryReadSingleHeader(
            HttpContext httpContext,
            string name,
            out string value)
        {
            var values = httpContext.Request.Headers[name];

            if (values.Count != 1 ||
                string.IsNullOrWhiteSpace(values[0]))
            {
                value = string.Empty;
                return false;
            }

            value = values[0]!;
            return true;
        }

        private static bool TryReadSessionToken(
            string authorizationValue,
            out string sessionToken)
        {
            const string prefix = AuthorizationScheme + " ";

            if (!authorizationValue.StartsWith(
                    prefix,
                    StringComparison.Ordinal) ||
                authorizationValue.Length == prefix.Length)
            {
                sessionToken = string.Empty;
                return false;
            }

            sessionToken = authorizationValue[prefix.Length..];

            return !string.IsNullOrWhiteSpace(sessionToken) &&
                !sessionToken.Any(char.IsWhiteSpace);
        }
    }
}
