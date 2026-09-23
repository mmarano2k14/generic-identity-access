using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;
using IdentityAccess.Mfa.Totp;

namespace IdentityAccess.Tests.Mfa.Totp
{
    internal sealed class TotpTestStore : ITotpAuthenticatorStore
    {
        private UserAuthenticator? _authenticator;

        public byte[]? ProtectedSecret { get; private set; }
        public long? LastAcceptedTimeStep { get; private set; }

        public UserAuthenticatorStatus? Status => _authenticator?.Status;

        public Task CreatePendingAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            byte[] protectedSecret,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _authenticator = authenticator;
            ProtectedSecret = protectedSecret.ToArray();
            LastAcceptedTimeStep = null;
            return Task.CompletedTask;
        }

        public Task<TotpAuthenticatorState?> GetAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_authenticator is null ||
                _authenticator.IdentityScopeId != identityScopeId ||
                _authenticator.UserId != userId ||
                _authenticator.AuthenticatorId != authenticatorId ||
                ProtectedSecret is null)
            {
                return Task.FromResult<TotpAuthenticatorState?>(null);
            }

            return Task.FromResult<TotpAuthenticatorState?>(
                new TotpAuthenticatorState(
                    _authenticator.Status,
                    ProtectedSecret.ToArray(),
                    TotpProviderOptions.AlgorithmName,
                    TotpProviderOptions.Digits,
                    TotpProviderOptions.PeriodSeconds,
                    LastAcceptedTimeStep));
        }

        public Task<TotpStoreMutationResult> TryConfirmAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            long acceptedTimeStep,
            DateTimeOffset confirmedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Matches(identityScopeId, userId, authenticatorId))
                return Task.FromResult(TotpStoreMutationResult.NotFound);
            if (_authenticator!.Status != UserAuthenticatorStatus.Pending)
                return Task.FromResult(TotpStoreMutationResult.InvalidState);
            if (LastAcceptedTimeStep is not null && acceptedTimeStep <= LastAcceptedTimeStep.Value)
                return Task.FromResult(TotpStoreMutationResult.ReplayDetected);

            LastAcceptedTimeStep = acceptedTimeStep;
            _authenticator = new UserAuthenticator(
                _authenticator.IdentityScopeId,
                _authenticator.AuthenticatorId,
                _authenticator.UserId,
                _authenticator.Provider,
                _authenticator.DisplayName,
                UserAuthenticatorStatus.Active,
                _authenticator.CreatedAt,
                confirmedAt,
                confirmedAt,
                revokedAt: null);
            return Task.FromResult(TotpStoreMutationResult.Succeeded);
        }

        public Task<TotpStoreMutationResult> TryRecordSuccessfulVerificationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            long acceptedTimeStep,
            DateTimeOffset usedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Matches(identityScopeId, userId, authenticatorId))
                return Task.FromResult(TotpStoreMutationResult.NotFound);
            if (_authenticator!.Status != UserAuthenticatorStatus.Active)
                return Task.FromResult(TotpStoreMutationResult.InvalidState);
            if (LastAcceptedTimeStep is not null && acceptedTimeStep <= LastAcceptedTimeStep.Value)
                return Task.FromResult(TotpStoreMutationResult.ReplayDetected);

            LastAcceptedTimeStep = acceptedTimeStep;
            _authenticator = new UserAuthenticator(
                _authenticator.IdentityScopeId,
                _authenticator.AuthenticatorId,
                _authenticator.UserId,
                _authenticator.Provider,
                _authenticator.DisplayName,
                _authenticator.Status,
                _authenticator.CreatedAt,
                _authenticator.ConfirmedAt,
                usedAt,
                revokedAt: null);
            return Task.FromResult(TotpStoreMutationResult.Succeeded);
        }

        private bool Matches(Guid identityScopeId, Guid userId, Guid authenticatorId) =>
            _authenticator is not null &&
            _authenticator.IdentityScopeId == identityScopeId &&
            _authenticator.UserId == userId &&
            _authenticator.AuthenticatorId == authenticatorId;
    }
}
