using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Provides application operations for credential administration.</summary>
    public sealed class CredentialAdministrationService(
        IDatabaseRouteResolver routeResolver,
        IPasswordCredentialStore credentials,
        ICredentialMutationStore credentialMutations,
        IPasswordHashingService passwordHasher,
        ISecurityAuditWriter auditWriter) : ICredentialAdministrationService
    {
        /// <summary>Gets password credential metadata for the requested user.</summary>
        public async Task<CredentialMetadata?> GetAsync(Guid identityScopeId, ApplicationKey application, Guid userId,
            CancellationToken cancellationToken)
        {
            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var record = await credentials.GetBySubjectAsync(route,
                new SubjectReference(identityScopeId, userId), cancellationToken).ConfigureAwait(false);
            return record is null ? null : Metadata(record);
        }

        /// <summary>Creates a password credential for the requested user.</summary>
        public async Task<CredentialMetadata> CreateAsync(Guid identityScopeId, ApplicationKey application, Guid userId,
            string loginIdentifier, string password, CancellationToken cancellationToken)
        {
            ValidatePassword(password);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var subject = new SubjectReference(identityScopeId, userId);
            var login = new LoginIdentifier(loginIdentifier);
            var hash = passwordHasher.Hash(subject, password);
            var created = await credentialMutations.CreateForExistingUserAsync(
                route,
                new PasswordCredential(subject, login, hash),
                cancellationToken).ConfigureAwait(false);

            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.PasswordCredentialCreated,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    userId: userId,
                    application: application,
                    targetId: userId.ToString("D")),
                cancellationToken).ConfigureAwait(false);

            return Metadata(created);
        }

        /// <summary>Changes the password credential using optimistic concurrency.</summary>
        public async Task<CredentialMetadata> ChangePasswordAsync(Guid identityScopeId, ApplicationKey application,
            Guid userId, string loginIdentifier, string password, long expectedVersion,
            CancellationToken cancellationToken)
        {
            ValidatePassword(password);
            var route = await ResolveAsync(identityScopeId, application, cancellationToken).ConfigureAwait(false);
            var subject = new SubjectReference(identityScopeId, userId);
            var login = new LoginIdentifier(loginIdentifier);
            var hash = passwordHasher.Hash(subject, password);
            var updated = await credentialMutations.UpdatePasswordAndRevokeSessionsAsync(
                route,
                new PasswordCredential(subject, login, hash),
                expectedVersion,
                cancellationToken).ConfigureAwait(false);

            await auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.PasswordChanged,
                    SecurityAuditOutcome.Succeeded,
                    identityScopeId,
                    userId: userId,
                    application: application,
                    targetId: userId.ToString("D")),
                cancellationToken).ConfigureAwait(false);

            return Metadata(updated);
        }

        private ValueTask<ResolvedDatabaseRoute> ResolveAsync(Guid identityScopeId, ApplicationKey application,
            CancellationToken cancellationToken) =>
            routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);

        private static CredentialMetadata Metadata(VersionedRecord<PasswordCredential> record) =>
            new(record.Value.Subject.UserId, record.Value.LoginIdentifier.Value, record.Value.FailedAccessCount,
                record.Value.LockoutUntil, record.Version);

        private static void ValidatePassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            if (password.Length is < 12 or > 256)
                throw new ArgumentException("Passwords must contain 12 to 256 characters.", nameof(password));
        }
    }
}
