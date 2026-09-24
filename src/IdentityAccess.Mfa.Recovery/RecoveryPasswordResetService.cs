using System.Security.Cryptography;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>
    /// Performs account recovery with an existing single-use recovery code without introducing a
    /// second password-reset token or external delivery channel.
    /// </summary>
    internal sealed class RecoveryPasswordResetService(
        IAuthenticationClientRegistry clients,
        IAuthenticationDirectoryLocator directoryLocator,
        IPasswordCredentialStore credentials,
        IPasswordHashingService passwordHasher,
        IRecoveryCodeStore store,
        IMfaProviderPolicyGuard policyGuard,
        TimeProvider timeProvider,
        ISecurityAuditWriter auditWriter)
        : IRecoveryPasswordResetService
    {
        /// <inheritdoc />
        public async Task<RecoveryPasswordResetDecision> ResetPasswordAsync(
            string clientId,
            string loginIdentifier,
            string recoveryCode,
            string newPassword,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(loginIdentifier);
            ArgumentNullException.ThrowIfNull(recoveryCode);
            ValidatePassword(newPassword);

            if (!clients.TryGet(clientId, out var client))
            {
                ConsumeUnknownWork(recoveryCode, newPassword);
                return RecoveryPasswordResetDecision.UnknownClient;
            }

            AuthenticationDirectoryLocation location;
            try
            {
                location = await directoryLocator.LocateAsync(
                    client.Application,
                    client.AuthenticationContextKey,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (DatabaseRouteException)
            {
                ConsumeUnknownWork(recoveryCode, newPassword);
                return RecoveryPasswordResetDecision.Unavailable;
            }

            LoginIdentifier login;
            try
            {
                login = new LoginIdentifier(loginIdentifier);
            }
            catch (ArgumentException)
            {
                ConsumeUnknownWork(recoveryCode, newPassword);
                await AuditFailureAsync(location.Route, client, null, cancellationToken).ConfigureAwait(false);
                return RecoveryPasswordResetDecision.Rejected;
            }

            var credential = await credentials.FindByLoginAsync(
                location.Route,
                login.NormalizedValue,
                cancellationToken).ConfigureAwait(false);

            if (credential is null)
            {
                ConsumeUnknownWork(recoveryCode, newPassword);
                await AuditFailureAsync(location.Route, client, null, cancellationToken).ConfigureAwait(false);
                return RecoveryPasswordResetDecision.Rejected;
            }

            var policy = await policyGuard.EvaluateAsync(
                location.Route,
                credential.Value.Subject.IdentityScopeId,
                client.Application,
                RecoveryAuthenticationFactorProviderKey.Instance,
                cancellationToken).ConfigureAwait(false);

            if (policy != MfaProviderPolicyDecision.Allowed)
            {
                ConsumeUnknownWork(recoveryCode, newPassword);
                await AuditFailureAsync(
                    location.Route,
                    client,
                    credential.Value.Subject.UserId,
                    cancellationToken).ConfigureAwait(false);
                return RecoveryPasswordResetDecision.Rejected;
            }

            if (passwordHasher.Verify(
                    credential.Value.Subject,
                    credential.Value.PasswordHash,
                    newPassword) != PasswordHashVerification.Failed)
            {
                ConsumeRecoveryCodeHash(recoveryCode);
                await auditWriter.TryWriteAsync(
                    location.Route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.PasswordRecoveryFailed,
                        SecurityAuditOutcome.Denied,
                        credential.Value.Subject.IdentityScopeId,
                        userId: credential.Value.Subject.UserId,
                        application: client.Application,
                        clientId: client.ClientId,
                        targetId: credential.Value.Subject.UserId.ToString("D"),
                        reasonCode: SecurityAuditReasonCode.PasswordReuseRejected),
                    cancellationToken).ConfigureAwait(false);
                return RecoveryPasswordResetDecision.PasswordReuseRejected;
            }

            if (!RecoveryCodeGenerator.TryHash(recoveryCode, out var codeHash))
            {
                passwordHasher.ConsumeUnknownCredential(newPassword);
                await AuditFailureAsync(
                    location.Route,
                    client,
                    credential.Value.Subject.UserId,
                    cancellationToken).ConfigureAwait(false);
                return RecoveryPasswordResetDecision.Rejected;
            }

            try
            {
                var passwordHash = passwordHasher.Hash(
                    credential.Value.Subject,
                    newPassword);

                var result = await store.TryResetPasswordAsync(
                    location.Route,
                    credential.Value.Subject.IdentityScopeId,
                    credential.Value.Subject.UserId,
                    codeHash,
                    passwordHash,
                    timeProvider.GetUtcNow(),
                    cancellationToken).ConfigureAwait(false);

                if (result != RecoveryPasswordResetStoreResult.Succeeded)
                {
                    await AuditFailureAsync(
                        location.Route,
                        client,
                        credential.Value.Subject.UserId,
                        cancellationToken).ConfigureAwait(false);
                    return RecoveryPasswordResetDecision.Rejected;
                }

                await auditWriter.TryWriteAsync(
                    location.Route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.PasswordRecoverySucceeded,
                        SecurityAuditOutcome.Succeeded,
                        credential.Value.Subject.IdentityScopeId,
                        userId: credential.Value.Subject.UserId,
                        application: client.Application,
                        clientId: client.ClientId,
                        targetId: credential.Value.Subject.UserId.ToString("D")),
                    cancellationToken).ConfigureAwait(false);

                return RecoveryPasswordResetDecision.Succeeded;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(codeHash);
            }
        }

        private void ConsumeUnknownWork(string recoveryCode, string newPassword)
        {
            ConsumeRecoveryCodeHash(recoveryCode);
            passwordHasher.ConsumeUnknownCredential(newPassword);
        }

        private static void ConsumeRecoveryCodeHash(string recoveryCode)
        {
            if (!RecoveryCodeGenerator.TryHash(recoveryCode, out var hash))
                return;

            CryptographicOperations.ZeroMemory(hash);
        }

        private Task<bool> AuditFailureAsync(
            ResolvedDatabaseRoute route,
            AuthenticationClientRegistration client,
            Guid? userId,
            CancellationToken cancellationToken) =>
            auditWriter.TryWriteAsync(
                route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.PasswordRecoveryFailed,
                    SecurityAuditOutcome.Denied,
                    route.Request.IdentityScopeId,
                    userId: userId,
                    application: client.Application,
                    clientId: client.ClientId,
                    targetId: userId?.ToString("D"),
                    reasonCode: SecurityAuditReasonCode.InvalidAuthenticationFactorProof),
                cancellationToken);

        private static void ValidatePassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            if (password.Length is < 12 or > 256)
                throw new ArgumentException("Passwords must contain 12 to 256 characters.", nameof(password));
        }
    }
}
