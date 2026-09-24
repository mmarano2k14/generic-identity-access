using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authentication
{
    internal sealed class FixedSessionStore(
        AuthenticationSession? session)
        : IAuthenticationSessionStore
    {
        public Task<bool> CreateForActiveUserAsync(
            ResolvedDatabaseRoute route,
            AuthenticationSession session,
            byte[] tokenHash,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AuthenticationSession?> ValidateAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            byte[] tokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AuthenticationSession?> ValidateReferenceAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(session);

        public Task<AuthenticationSession?> UpgradeAssuranceAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            string factorMethodReference,
            DateTimeOffset verifiedAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RevokeAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            Guid sessionId,
            byte[] tokenHash,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> RevokeAllForSubjectAsync(
            ResolvedDatabaseRoute route,
            SubjectReference subject,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> RevokeAllForClientAsync(
            ResolvedDatabaseRoute route,
            string clientId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
