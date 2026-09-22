using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Api.Oidc
{
    /// <summary>
    /// Resolves the already-authenticated local user session used by the OIDC authorization
    /// endpoint without exposing the raw session token beyond the HTTP transport boundary.
    /// </summary>
    public interface IOidcLocalSessionResolver
    {
        /// <summary>Resolves a validated local session for the requested registered client.</summary>
        ValueTask<AuthenticatedSessionContext?> ResolveAsync(
            HttpContext httpContext,
            string? clientId,
            CancellationToken cancellationToken);
    }
}
