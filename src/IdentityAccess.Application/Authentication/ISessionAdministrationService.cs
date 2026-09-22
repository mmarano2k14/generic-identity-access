using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Defines administrative bulk-revocation operations for local authentication sessions.
    /// </summary>
    public interface ISessionAdministrationService
    {
        /// <summary>Revokes every active session for the requested user.</summary>
        Task<int> RevokeUserSessionsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid userId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Revokes every active session for a registered client that belongs to the requested
        /// application.
        /// </summary>
        Task<int> RevokeClientSessionsAsync(
            Guid identityScopeId,
            ApplicationKey application,
            string clientId,
            CancellationToken cancellationToken);
    }
}
