using IdentityAccess.Application.Routing;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Revalidates bearer-token session continuity through the trusted authentication-directory
    /// route and current persisted session/user state.
    /// </summary>
    public sealed class OidcAccessTokenSessionValidator(
        IAuthenticationDirectoryLocator directoryLocator,
        IAuthenticationSessionStore sessions,
        TimeProvider timeProvider)
        : IOidcAccessTokenSessionValidator
    {
        /// <inheritdoc />
        public async Task<bool> ValidateAsync(
            ValidatedOidcAccessToken token,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(token);
            cancellationToken.ThrowIfCancellationRequested();

            var location = await directoryLocator
                .LocateAsync(
                    token.Application,
                    token.AuthenticationContextKey,
                    cancellationToken)
                .ConfigureAwait(false);

            if (location.Route.Request.IdentityScopeId !=
                    token.Subject.IdentityScopeId ||
                location.Route.Request.Application !=
                    token.Application ||
                !string.Equals(
                    location.AuthenticationContextKey,
                    token.AuthenticationContextKey,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var session = await sessions
                .ValidateReferenceAsync(
                    location.Route,
                    token.ClientId,
                    token.SessionId,
                    timeProvider.GetUtcNow(),
                    cancellationToken)
                .ConfigureAwait(false);

            return session is not null &&
                session.Subject == token.Subject &&
                session.Application == token.Application &&
                string.Equals(
                    session.AuthenticationContextKey,
                    token.AuthenticationContextKey,
                    StringComparison.Ordinal) &&
                session.Assurance.Level >= token.Assurance.Level &&
                session.Assurance.VerifiedAt >= token.Assurance.VerifiedAt &&
                token.Assurance.Methods.All(method =>
                    session.Assurance.Methods.Contains(method, StringComparer.Ordinal));
        }
    }
}
