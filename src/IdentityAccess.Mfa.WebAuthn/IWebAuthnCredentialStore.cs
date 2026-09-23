using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Provider-owned persistence boundary for WebAuthn challenges and public credentials.</summary>
    internal interface IWebAuthnCredentialStore
    {
        Task CreatePendingRegistrationAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            ApplicationKey application,
            byte[] challengeHash,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken);

        Task<WebAuthnPendingRegistrationState?> GetPendingRegistrationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<byte[]>> ListActiveCredentialIdsAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            CancellationToken cancellationToken);

        Task<WebAuthnRegistrationStoreResult> TryCompleteRegistrationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            ApplicationKey application,
            WebAuthnCredentialMaterial credential,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken);
    }
}
