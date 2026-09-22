using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Models one rotating refresh-token family for protocol orchestration tests.</summary>
    internal sealed class OidcTestRefreshTokenStore :
        IOidcRefreshTokenStore
    {
        private readonly List<byte[]> consumedHashes = [];
        private OidcRefreshTokenGrant? currentGrant;
        private byte[]? currentHash;

        /// <summary>Gets or sets whether initial family creation is allowed.</summary>
        public bool AllowCreate { get; set; } = true;

        /// <summary>Gets or sets whether current local session/user state remains eligible.</summary>
        public bool RotationEligible { get; set; } = true;

        /// <summary>Gets whether replay detection revoked the family.</summary>
        public bool FamilyRevoked { get; private set; }

        /// <summary>Gets the initial family identifier when one has been created.</summary>
        public Guid? FamilyId { get; private set; }

        /// <summary>Gets the current refresh-token grant.</summary>
        public OidcRefreshTokenGrant? CurrentGrant => currentGrant;

        /// <inheritdoc />
        public Task<bool> CreateFamilyForActiveSessionAsync(
            ResolvedDatabaseRoute route,
            OidcRefreshTokenGrant grant,
            byte[] tokenHash,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!AllowCreate)
            {
                return Task.FromResult(false);
            }

            FamilyId = grant.FamilyId;
            currentGrant = grant;
            currentHash = tokenHash.ToArray();
            consumedHashes.Clear();
            FamilyRevoked = false;

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<OidcRefreshTokenRotationResult> RotateAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            byte[] presentedTokenHash,
            Guid replacementTokenId,
            byte[] replacementTokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (FamilyRevoked)
            {
                return Task.FromResult(
                    OidcRefreshTokenRotationResult.Invalid());
            }

            if (consumedHashes.Any(hash => hash.SequenceEqual(presentedTokenHash)))
            {
                FamilyRevoked = true;
                return Task.FromResult(
                    OidcRefreshTokenRotationResult.ReuseDetected());
            }

            if (!RotationEligible ||
                currentGrant is null ||
                currentHash is null ||
                !currentHash.SequenceEqual(presentedTokenHash) ||
                !string.Equals(
                    currentGrant.ClientId,
                    clientId,
                    StringComparison.Ordinal) ||
                currentGrant.ExpiresAt <= now)
            {
                return Task.FromResult(
                    OidcRefreshTokenRotationResult.Invalid());
            }

            consumedHashes.Add(
                currentHash.ToArray());

            var replacement =
                new OidcRefreshTokenGrant(
                    currentGrant.FamilyId,
                    replacementTokenId,
                    currentGrant.TokenId,
                    checked(currentGrant.SequenceNumber + 1),
                    currentGrant.Subject,
                    currentGrant.SessionId,
                    currentGrant.ClientId,
                    currentGrant.Application,
                    currentGrant.AuthenticationContextKey,
                    currentGrant.Scope,
                    currentGrant.AuthenticatedAt,
                    now,
                    currentGrant.ExpiresAt);

            currentGrant = replacement;
            currentHash = replacementTokenHash.ToArray();

            return Task.FromResult(
                OidcRefreshTokenRotationResult.Rotated(
                    replacement));
        }
    }
}
