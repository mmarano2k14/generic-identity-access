

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines the contract for local authentication service.</summary>
    public interface ILocalAuthenticationService
    {
        /// <summary>Authenticates a local password credential and issues an opaque session on success.</summary>
        Task<PasswordLoginResult> LoginAsync(string clientId, string loginIdentifier, string password,
            string redirectUri, CancellationToken cancellationToken);

        /// <summary>Validates an opaque local authentication session.</summary>
        Task<SessionValidationResult> ValidateSessionAsync(string clientId, Guid sessionId, string sessionToken,
            CancellationToken cancellationToken);

        /// <summary>Revokes an opaque local session and validates an optional post-logout redirect URI.</summary>
        Task<LogoutResult> LogoutAsync(string clientId, Guid sessionId, string sessionToken,
            string? postLogoutRedirectUri, CancellationToken cancellationToken);
    }
}
