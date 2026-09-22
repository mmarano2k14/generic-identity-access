using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Resolves administration identity from a self-contained OIDC Bearer access token, then
    /// revalidates the token's local-session reference against current session and user state. The
    /// resolver never derives trust from caller-selected client, application, or identity-scope
    /// headers; those bindings come only from the validated token and server client registry.
    /// </summary>
    internal sealed class BearerAdministrationRequestContextResolver(
        IOidcAccessTokenValidator validator,
        IOidcAccessTokenSessionValidator sessionValidator,
        ILogger<BearerAdministrationRequestContextResolver> logger)
    {
        internal const string AuthorizationScheme = "Bearer";

        /// <summary>Validates one bearer credential and projects a trusted administration context.</summary>
        public async ValueTask<AdministrationAuthenticationResult> ResolveAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            cancellationToken.ThrowIfCancellationRequested();

            if (httpContext.Request.Headers.ContainsKey(
                    LocalSessionAdministrationRequestContextResolver.ClientHeaderName) ||
                httpContext.Request.Headers.ContainsKey(
                    LocalSessionAdministrationRequestContextResolver.SessionHeaderName))
            {
                return AdministrationAuthenticationResult.Unauthenticated(
                    AdministrationAuthenticationFailureCode.CredentialsMalformed);
            }

            var values = httpContext.Request.Headers["Authorization"];

            if (values.Count != 1 ||
                string.IsNullOrWhiteSpace(values[0]) ||
                !TryReadBearerToken(values[0]!, out var accessToken))
            {
                return AdministrationAuthenticationResult.Unauthenticated(
                    AdministrationAuthenticationFailureCode.BearerTokenInvalid);
            }

            OidcAccessTokenValidationResult validation;

            try
            {
                validation =
                    validator.Validate(
                        accessToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "OIDC bearer access-token validation failed technically.");

                return AdministrationAuthenticationResult.Unavailable(
                    AdministrationAuthenticationFailureCode.BearerValidationUnavailable);
            }

            if (!validation.Valid ||
                validation.Token is null)
            {
                return AdministrationAuthenticationResult.Unauthenticated(
                    AdministrationAuthenticationFailureCode.BearerTokenInvalid);
            }

            bool sessionValid;

            try
            {
                sessionValid = await sessionValidator
                    .ValidateAsync(
                        validation.Token,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "OIDC bearer session-continuity validation failed technically.");

                return AdministrationAuthenticationResult.Unavailable(
                    AdministrationAuthenticationFailureCode.BearerValidationUnavailable);
            }

            if (!sessionValid)
            {
                return AdministrationAuthenticationResult.Unauthenticated(
                    AdministrationAuthenticationFailureCode.BearerTokenInvalid);
            }

            var token = validation.Token;

            return AdministrationAuthenticationResult.Authenticated(
                new AdministrationRequestContext(
                    token.Subject,
                    token.SessionId,
                    token.ClientId,
                    token.Application,
                    token.AuthenticationContextKey,
                    token.ExpiresAt));
        }

        private static bool TryReadBearerToken(
            string authorizationValue,
            out string accessToken)
        {
            const string prefix = AuthorizationScheme + " ";

            if (!authorizationValue.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase) ||
                authorizationValue.Length == prefix.Length)
            {
                accessToken = string.Empty;
                return false;
            }

            accessToken = authorizationValue[prefix.Length..];

            return !string.IsNullOrWhiteSpace(accessToken) &&
                !accessToken.Any(char.IsWhiteSpace);
        }
    }
}
