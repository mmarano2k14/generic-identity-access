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

        public UserAuthenticatorStatus? Status => _authenticator?.Status;
        public WebAuthnCredentialMaterial? Credential { get; private set; }

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
            return Task.FromResult(WebAuthnRegistrationStoreResult.Succeeded);
        }

        private bool Matches(Guid identityScopeId, Guid userId, Guid authenticatorId) =>
            _authenticator is not null &&
            _authenticator.IdentityScopeId == identityScopeId &&
            _authenticator.UserId == userId &&
            _authenticator.AuthenticatorId == authenticatorId;
    }
}
