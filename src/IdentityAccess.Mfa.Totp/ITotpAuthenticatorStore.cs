using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Provider-owned persistence boundary for TOTP secrets and atomic lifecycle transitions.</summary>
    internal interface ITotpAuthenticatorStore
    {
        Task CreatePendingAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            byte[] protectedSecret,
            CancellationToken cancellationToken);

        Task<TotpAuthenticatorState?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            CancellationToken cancellationToken);

        Task<TotpStoreMutationResult> TryConfirmAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            long acceptedTimeStep,
            DateTimeOffset confirmedAt,
            CancellationToken cancellationToken);

        Task<TotpStoreMutationResult> TryRecordSuccessfulVerificationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            long acceptedTimeStep,
            DateTimeOffset usedAt,
            CancellationToken cancellationToken);
    }
}
