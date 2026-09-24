using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;
using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    internal sealed class WebAuthnTestStore : IWebAuthnCredentialStore
    {
        private UserAuthenticator? _authenticator;
        private ApplicationKey? _application;
        private byte[]? _challengeHash;
        private DateTimeOffset _expiresAt;
        private DateTimeOffset? _consumedAt;
        private readonly List<byte[]> _existingCredentialIds = [];
        private WebAuthnCredentialRecord? _credentialRecord;
        private readonly Dictionary<Guid, (Guid UserId, ApplicationKey Application, byte[] ChallengeHash, DateTimeOffset ExpiresAt, DateTimeOffset? ConsumedAt)> _authenticationChallenges = [];

        public UserAuthenticatorStatus? Status => _authenticator?.Status;
        public WebAuthnCredentialMaterial? Credential { get; private set; }
        public WebAuthnCredentialRecord? CredentialRecord => _credentialRecord;
        public DateTimeOffset? LastUsedAt { get; private set; }

        public Task CreatePendingRegistrationAsync(
            ResolvedDatabaseRoute route,
            UserAuthenticator authenticator,
            ApplicationKey application,
            byte[] challengeHash,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _authenticator = authenticator;
            _application = application;
            _challengeHash = challengeHash.ToArray();
            _expiresAt = expiresAt;
            _consumedAt = null;
            return Task.CompletedTask;
        }

        public Task<WebAuthnPendingRegistrationState?> GetPendingRegistrationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Matches(identityScopeId, userId, authenticatorId) ||
                _application is null || _challengeHash is null)
            {
                return Task.FromResult<WebAuthnPendingRegistrationState?>(null);
            }

            return Task.FromResult<WebAuthnPendingRegistrationState?>(
                new WebAuthnPendingRegistrationState(
                    _authenticator!.Status,
                    _application,
                    _challengeHash,
                    _expiresAt,
                    _consumedAt));
        }

        public Task<IReadOnlyList<byte[]>> ListActiveCredentialIdsAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<byte[]> copy = _existingCredentialIds.Select(value => value.ToArray()).ToArray();
            return Task.FromResult(copy);
        }

        public Task<WebAuthnRegistrationStoreResult> TryCompleteRegistrationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            ApplicationKey application,
            WebAuthnCredentialMaterial credential,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Matches(identityScopeId, userId, authenticatorId))
                return Task.FromResult(WebAuthnRegistrationStoreResult.NotFound);
            if (_consumedAt is not null)
                return Task.FromResult(WebAuthnRegistrationStoreResult.AlreadyUsed);
            if (_authenticator!.Status != UserAuthenticatorStatus.Pending ||
                _application is null ||
                !string.Equals(_application.Value, application.Value, StringComparison.Ordinal))
            {
                return Task.FromResult(WebAuthnRegistrationStoreResult.InvalidState);
            }
            if (completedAt > _expiresAt)
                return Task.FromResult(WebAuthnRegistrationStoreResult.Expired);
            if (_existingCredentialIds.Any(value => value.SequenceEqual(credential.CredentialId)))
                return Task.FromResult(WebAuthnRegistrationStoreResult.CredentialAlreadyRegistered);

            Credential = credential;
            _existingCredentialIds.Add(credential.CredentialId.ToArray());
            _consumedAt = completedAt;
            _authenticator = new UserAuthenticator(
                _authenticator.IdentityScopeId,
                _authenticator.AuthenticatorId,
                _authenticator.UserId,
                _authenticator.Provider,
                _authenticator.DisplayName,
                UserAuthenticatorStatus.Active,
                _authenticator.CreatedAt,
                completedAt,
                lastUsedAt: null,
                revokedAt: null);
            _credentialRecord = new WebAuthnCredentialRecord(
                authenticatorId,
                UserAuthenticatorStatus.Active,
                credential.CredentialId,
                credential.CosePublicKey,
                credential.CoseAlgorithm,
                credential.SignCount,
                credential.BackupEligible,
                credential.BackupState,
                credential.UserHandle);
            return Task.FromResult(WebAuthnRegistrationStoreResult.Succeeded);
        }

        public Task CreateAuthenticationChallengeAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid challengeId,
            ApplicationKey application,
            byte[] challengeHash,
            DateTimeOffset createdAt,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _authenticationChallenges[challengeId] = (
                userId,
                application,
                challengeHash.ToArray(),
                expiresAt,
                null);
            return Task.CompletedTask;
        }

        public Task<WebAuthnAuthenticationChallengeState?> GetAuthenticationChallengeAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid challengeId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_authenticationChallenges.TryGetValue(challengeId, out var state) || state.UserId != userId)
                return Task.FromResult<WebAuthnAuthenticationChallengeState?>(null);

            return Task.FromResult<WebAuthnAuthenticationChallengeState?>(
                new WebAuthnAuthenticationChallengeState(
                    state.Application,
                    state.ChallengeHash,
                    state.ExpiresAt,
                    state.ConsumedAt));
        }

        public Task<IReadOnlyList<WebAuthnCredentialRecord>> ListActiveCredentialsAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<WebAuthnCredentialRecord> result =
                _credentialRecord is not null &&
                _authenticator is not null &&
                _authenticator.IdentityScopeId == identityScopeId &&
                _authenticator.UserId == userId &&
                _credentialRecord.Status == UserAuthenticatorStatus.Active
                    ? [_credentialRecord]
                    : [];
            return Task.FromResult(result);
        }

        public Task<WebAuthnCredentialRecord?> GetActiveCredentialAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            byte[] credentialId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_credentialRecord is null ||
                _authenticator is null ||
                _authenticator.IdentityScopeId != identityScopeId ||
                _authenticator.UserId != userId ||
                _credentialRecord.Status != UserAuthenticatorStatus.Active ||
                !_credentialRecord.CredentialId.SequenceEqual(credentialId))
            {
                return Task.FromResult<WebAuthnCredentialRecord?>(null);
            }

            return Task.FromResult<WebAuthnCredentialRecord?>(_credentialRecord);
        }

        public Task<WebAuthnAuthenticationStoreResult> TryCompleteAuthenticationAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            Guid userId,
            Guid challengeId,
            ApplicationKey application,
            byte[] credentialId,
            long assertedSignCount,
            bool assertedBackupEligible,
            bool assertedBackupState,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_authenticationChallenges.TryGetValue(challengeId, out var challenge) ||
                challenge.UserId != userId ||
                _credentialRecord is null ||
                _authenticator is null ||
                _authenticator.IdentityScopeId != identityScopeId ||
                _authenticator.UserId != userId ||
                !_credentialRecord.CredentialId.SequenceEqual(credentialId))
            {
                return Task.FromResult(WebAuthnAuthenticationStoreResult.NotFound);
            }

            if (_credentialRecord.Status != UserAuthenticatorStatus.Active ||
                !string.Equals(challenge.Application.Value, application.Value, StringComparison.Ordinal) ||
                _credentialRecord.BackupEligible != assertedBackupEligible)
            {
                return Task.FromResult(WebAuthnAuthenticationStoreResult.InvalidState);
            }

            if (challenge.ConsumedAt is not null)
                return Task.FromResult(WebAuthnAuthenticationStoreResult.AlreadyUsed);
            if (completedAt > challenge.ExpiresAt)
                return Task.FromResult(WebAuthnAuthenticationStoreResult.Expired);
            if ((_credentialRecord.SignCount != 0 || assertedSignCount != 0) &&
                assertedSignCount <= _credentialRecord.SignCount)
            {
                return Task.FromResult(WebAuthnAuthenticationStoreResult.ReplayDetected);
            }

            _credentialRecord = new WebAuthnCredentialRecord(
                _credentialRecord.AuthenticatorId,
                _credentialRecord.Status,
                _credentialRecord.CredentialId,
                _credentialRecord.CosePublicKey,
                _credentialRecord.CoseAlgorithm,
                assertedSignCount,
                _credentialRecord.BackupEligible,
                assertedBackupState,
                _credentialRecord.UserHandle);
            LastUsedAt = completedAt;
            _authenticationChallenges[challengeId] = (
                challenge.UserId,
                challenge.Application,
                challenge.ChallengeHash,
                challenge.ExpiresAt,
                completedAt);
            return Task.FromResult(WebAuthnAuthenticationStoreResult.Succeeded);
        }

        public void SeedActiveCredential(
            Guid identityScopeId,
            Guid userId,
            Guid authenticatorId,
            WebAuthnCredentialMaterial credential,
            DateTimeOffset confirmedAt)
        {
            _authenticator = new UserAuthenticator(
                identityScopeId,
                authenticatorId,
                userId,
                WebAuthnAuthenticationFactorProviderKey.Instance,
                "Passkey",
                UserAuthenticatorStatus.Active,
                confirmedAt,
                confirmedAt,
                lastUsedAt: null,
                revokedAt: null);
            Credential = credential;
            _credentialRecord = new WebAuthnCredentialRecord(
                authenticatorId,
                UserAuthenticatorStatus.Active,
                credential.CredentialId,
                credential.CosePublicKey,
                credential.CoseAlgorithm,
                credential.SignCount,
                credential.BackupEligible,
                credential.BackupState,
                credential.UserHandle);
            _existingCredentialIds.Clear();
            _existingCredentialIds.Add(credential.CredentialId.ToArray());
        }

        private bool Matches(Guid identityScopeId, Guid userId, Guid authenticatorId) =>
            _authenticator is not null &&
            _authenticator.IdentityScopeId == identityScopeId &&
            _authenticator.UserId == userId &&
            _authenticator.AuthenticatorId == authenticatorId;
    }
}
