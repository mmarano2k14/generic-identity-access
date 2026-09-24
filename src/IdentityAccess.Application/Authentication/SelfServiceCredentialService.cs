using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Re-authenticates the current local-session subject before a sensitive password replacement.
    /// </summary>
    public sealed class SelfServiceCredentialService(
        IAuthenticationDirectoryLocator directoryLocator,
        IPasswordCredentialStore credentials,
        ICredentialMutationStore credentialMutations,
        IPasswordHashingService passwordHasher,
        IAuthenticationAssuranceService assuranceService,
        AuthenticationOptions options,
        ISecurityAuditWriter auditWriter)
        : ISelfServiceCredentialService
    {
        /// <inheritdoc />
        public async Task<SelfServicePasswordChangeDecision> ChangePasswordAsync(
            AuthenticatedSessionContext session,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(currentPassword);
            ValidatePassword(newPassword);

            var evaluation = await assuranceService.EvaluateAsync(
                session,
                TimeSpan.FromMinutes(options.SensitiveOperationMfaMaxAgeMinutes),
                cancellationToken).ConfigureAwait(false);

            if (evaluation is null)
                return SelfServicePasswordChangeDecision.InvalidSession;

            if (evaluation.PolicyMode == MfaPolicyMode.Required && !evaluation.Fresh)
            {
                await AuditRejectedAsync(
                    session,
                    SecurityAuditReasonCode.RecentAuthenticationRequired,
                    cancellationToken).ConfigureAwait(false);
                return SelfServicePasswordChangeDecision.RecentMfaRequired;
            }

            AuthenticationDirectoryLocation location;
            try
            {
                location = await directoryLocator.LocateAsync(
                    session.Application,
                    session.AuthenticationContextKey,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return SelfServicePasswordChangeDecision.CredentialUnavailable;
            }

            if (location.Route.Request.IdentityScopeId != session.Subject.IdentityScopeId ||
                location.Route.Request.Application != session.Application ||
                !string.Equals(
                    location.AuthenticationContextKey,
                    session.AuthenticationContextKey,
                    StringComparison.Ordinal))
            {
                return SelfServicePasswordChangeDecision.InvalidSession;
            }

            var credential = await credentials.GetBySubjectAsync(
                location.Route,
                session.Subject,
                cancellationToken).ConfigureAwait(false);

            if (credential is null)
                return SelfServicePasswordChangeDecision.CredentialUnavailable;

            if (passwordHasher.Verify(
                    session.Subject,
                    credential.Value.PasswordHash,
                    currentPassword) == PasswordHashVerification.Failed)
            {
                await AuditRejectedAsync(
                    session,
                    SecurityAuditReasonCode.InvalidCredentials,
                    cancellationToken).ConfigureAwait(false);
                return SelfServicePasswordChangeDecision.InvalidCurrentPassword;
            }

            if (passwordHasher.Verify(
                    session.Subject,
                    credential.Value.PasswordHash,
                    newPassword) != PasswordHashVerification.Failed)
            {
                await AuditRejectedAsync(
                    session,
                    SecurityAuditReasonCode.PasswordReuseRejected,
                    cancellationToken).ConfigureAwait(false);
                return SelfServicePasswordChangeDecision.PasswordReuseRejected;
            }

            var replacement = new PasswordCredential(
                session.Subject,
                credential.Value.LoginIdentifier,
                passwordHasher.Hash(session.Subject, newPassword));

            try
            {
                await credentialMutations.UpdatePasswordAndRevokeSessionsAsync(
                    location.Route,
                    replacement,
                    credential.Version,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (IdentityConcurrencyException)
            {
                return SelfServicePasswordChangeDecision.ConcurrencyConflict;
            }

            await auditWriter.TryWriteAsync(
                location.Route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.PasswordChanged,
                    SecurityAuditOutcome.Succeeded,
                    session.Subject.IdentityScopeId,
                    userId: session.Subject.UserId,
                    application: session.Application,
                    clientId: session.ClientId,
                    targetId: session.Subject.UserId.ToString("D")),
                cancellationToken).ConfigureAwait(false);

            return SelfServicePasswordChangeDecision.Succeeded;
        }

        private async Task AuditRejectedAsync(
            AuthenticatedSessionContext session,
            SecurityAuditReasonCode reasonCode,
            CancellationToken cancellationToken)
        {
            AuthenticationDirectoryLocation location;
            try
            {
                location = await directoryLocator.LocateAsync(
                    session.Application,
                    session.AuthenticationContextKey,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                return;
            }

            await auditWriter.TryWriteAsync(
                location.Route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.PasswordChangeRejected,
                    SecurityAuditOutcome.Denied,
                    session.Subject.IdentityScopeId,
                    userId: session.Subject.UserId,
                    application: session.Application,
                    clientId: session.ClientId,
                    targetId: session.Subject.UserId.ToString("D"),
                    reasonCode: reasonCode),
                cancellationToken).ConfigureAwait(false);
        }

        internal static void ValidatePassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            if (password.Length is < 12 or > 256)
                throw new ArgumentException("Passwords must contain 12 to 256 characters.", nameof(password));
        }
    }
}
