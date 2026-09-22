using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Stores one authorization-code grant in memory for OIDC protocol tests.</summary>
    internal sealed class OidcTestAuthorizationCodeStore :
        IOidcAuthorizationCodeStore
    {
        private OidcAuthorizationCodeGrant? grant;
        private byte[]? codeHash;
        private bool consumed;

        /// <summary>Gets or sets whether the active-session issuance predicate succeeds.</summary>
        public bool AllowCreate { get; set; } = true;

        /// <summary>Gets the most recently persisted grant.</summary>
        public OidcAuthorizationCodeGrant? Grant => grant;

        /// <summary>Gets the most recently persisted SHA-256 code hash.</summary>
        public byte[]? CodeHash => codeHash;

        /// <inheritdoc />
        public Task<bool> CreateForActiveSessionAsync(
            ResolvedDatabaseRoute route,
            OidcAuthorizationCodeGrant value,
            byte[] hash,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!AllowCreate)
            {
                return Task.FromResult(false);
            }

            grant = value;
            codeHash = hash.ToArray();
            consumed = false;

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<OidcAuthorizationCodeGrant?> ConsumeAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            string redirectUri,
            byte[] hash,
            string expectedCodeChallenge,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (consumed ||
                grant is null ||
                codeHash is null ||
                !codeHash.SequenceEqual(hash) ||
                !string.Equals(grant.ClientId, clientId, StringComparison.Ordinal) ||
                !string.Equals(grant.RedirectUri, redirectUri, StringComparison.Ordinal) ||
                !string.Equals(grant.CodeChallenge, expectedCodeChallenge, StringComparison.Ordinal) ||
                grant.ExpiresAt <= now)
            {
                return Task.FromResult<OidcAuthorizationCodeGrant?>(null);
            }

            consumed = true;

            return Task.FromResult<OidcAuthorizationCodeGrant?>(
                new OidcAuthorizationCodeGrant(
                    grant.CodeId,
                    grant.Subject,
                    grant.SessionId,
                    grant.ClientId,
                    grant.Application,
                    grant.AuthenticationContextKey,
                    grant.RedirectUri,
                    grant.Scope,
                    grant.CodeChallenge,
                    grant.Nonce,
                    grant.AuthenticatedAt,
                    grant.IssuedAt,
                    grant.ExpiresAt,
                    now));
        }
    }
}
