using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;
using IdentityAccess.Mfa.Recovery;

namespace IdentityAccess.Tests.Mfa.Recovery
{
    internal sealed class RecoveryTestStore : IRecoveryCodeStore
    {
        private readonly Dictionary<Guid, RecoveryTestSet> _sets = [];

        public IReadOnlyDictionary<Guid, RecoveryTestSet> Sets => _sets;

        public Task<bool> ReplaceActiveSetAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            IReadOnlyList<byte[]> codeHashes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var replaced = false;

            foreach (var pair in _sets.ToArray())
            {
                var current = pair.Value.Authenticator;
                if (current.IdentityScopeId != authenticator.IdentityScopeId ||
                    current.UserId != authenticator.UserId ||
                    current.Status != UserAuthenticatorStatus.Active)
                {
                    continue;
                }

                replaced = true;
                pair.Value.Authenticator = new UserAuthenticator(
                    current.IdentityScopeId,
                    current.AuthenticatorId,
                    current.UserId,
                    current.Provider,
                    current.DisplayName,
                    UserAuthenticatorStatus.Revoked,
                    current.CreatedAt,
                    current.ConfirmedAt,
                    current.LastUsedAt,
                    authenticator.CreatedAt);
            }

            _sets[authenticator.AuthenticatorId] = new RecoveryTestSet(
                authenticator,
                codeHashes.Select(hash => Convert.ToHexString(hash)).ToArray());
            return Task.FromResult(replaced);
        }

        public Task<RecoveryCodeStoreMutationResult> TryConsumeAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            byte[] codeHash,
            DateTimeOffset consumedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_sets.TryGetValue(authenticatorId, out var set) ||
                set.Authenticator.IdentityScopeId != identityScopeId ||
                set.Authenticator.UserId != userId)
            {
                return Task.FromResult(RecoveryCodeStoreMutationResult.NotFound);
            }

            if (set.Authenticator.Status != UserAuthenticatorStatus.Active)
                return Task.FromResult(RecoveryCodeStoreMutationResult.NotActive);

            var hash = Convert.ToHexString(codeHash);
            if (!set.Consumed.TryGetValue(hash, out var existingConsumedAt))
                return Task.FromResult(RecoveryCodeStoreMutationResult.InvalidCode);
            if (existingConsumedAt is not null)
                return Task.FromResult(RecoveryCodeStoreMutationResult.AlreadyConsumed);

            set.Consumed[hash] = consumedAt;
            var current = set.Authenticator;
            set.Authenticator = new UserAuthenticator(
                current.IdentityScopeId,
                current.AuthenticatorId,
                current.UserId,
                current.Provider,
                current.DisplayName,
                current.Status,
                current.CreatedAt,
                current.ConfirmedAt,
                consumedAt,
                current.RevokedAt);
            return Task.FromResult(RecoveryCodeStoreMutationResult.Succeeded);
        }

        public Task<RecoveryPasswordResetStoreResult> TryResetPasswordAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            byte[] codeHash,
            string passwordHash,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var set = _sets.Values.SingleOrDefault(candidate =>
                candidate.Authenticator.IdentityScopeId == identityScopeId &&
                candidate.Authenticator.UserId == userId &&
                candidate.Authenticator.Status == UserAuthenticatorStatus.Active);

            if (set is null)
                return Task.FromResult(RecoveryPasswordResetStoreResult.NotFound);

            var hash = Convert.ToHexString(codeHash);
            if (!set.Consumed.TryGetValue(hash, out var existingConsumedAt))
                return Task.FromResult(RecoveryPasswordResetStoreResult.InvalidCode);
            if (existingConsumedAt is not null)
                return Task.FromResult(RecoveryPasswordResetStoreResult.AlreadyConsumed);

            set.Consumed[hash] = occurredAt;
            var current = set.Authenticator;
            set.Authenticator = new UserAuthenticator(
                current.IdentityScopeId,
                current.AuthenticatorId,
                current.UserId,
                current.Provider,
                current.DisplayName,
                current.Status,
                current.CreatedAt,
                current.ConfirmedAt,
                occurredAt,
                current.RevokedAt);
            return Task.FromResult(RecoveryPasswordResetStoreResult.Succeeded);
        }

    }
}
