using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Revalidates local session provenance before evaluating or upgrading durable authentication assurance.
    /// </summary>
    public sealed class AuthenticationAssuranceService(
        IAuthenticationDirectoryLocator directoryLocator,
        IAuthenticationSessionStore sessions,
        IMfaPolicyStore policies,
        TimeProvider timeProvider,
        ISecurityAuditWriter auditWriter)
        : IAuthenticationAssuranceService
    {
        /// <inheritdoc />
        public async Task<AuthenticationAssuranceEvaluation?> EvaluateAsync(
            AuthenticatedSessionContext session,
            TimeSpan maximumMfaAge,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(session);
            if (maximumMfaAge <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maximumMfaAge));

            var location = await ResolveAsync(session, cancellationToken).ConfigureAwait(false);
            if (location is null)
                return null;

            var now = timeProvider.GetUtcNow();
            var current = await sessions.ValidateReferenceAsync(
                location.Route,
                session.ClientId,
                session.SessionId,
                now,
                cancellationToken).ConfigureAwait(false);

            if (!Matches(session, current))
                return null;

            var policy = await policies.GetAsync(
                location.Route,
                session.Subject.IdentityScopeId,
                session.Application,
                cancellationToken).ConfigureAwait(false);

            var policyMode = policy?.Value.Mode;
            var required = policyMode == MfaPolicyMode.Required;
            var fresh = current!.Assurance.IsRecentMultiFactor(now, maximumMfaAge);

            return new AuthenticationAssuranceEvaluation(
                policyMode,
                current.Assurance,
                !required || fresh,
                fresh);
        }

        /// <inheritdoc />
        public async Task<AuthenticatedSessionContext?> RecordFactorAsync(
            AuthenticatedSessionContext session,
            string factorMethodReference,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(session);
            var factor = AuthenticationMethodReferences.ValidateFactor(factorMethodReference);

            var location = await ResolveAsync(session, cancellationToken).ConfigureAwait(false);
            if (location is null)
                return null;

            var now = timeProvider.GetUtcNow();
            var updated = await sessions.UpgradeAssuranceAsync(
                location.Route,
                session.Subject,
                session.SessionId,
                session.ClientId,
                session.Application,
                session.AuthenticationContextKey,
                factor,
                now,
                cancellationToken).ConfigureAwait(false);

            if (updated is null)
            {
                await auditWriter.TryWriteAsync(
                    location.Route,
                    new SecurityAuditEvent(
                        SecurityAuditEventType.SessionAssuranceUpgradeFailed,
                        SecurityAuditOutcome.Denied,
                        session.Subject.IdentityScopeId,
                        userId: session.Subject.UserId,
                        application: session.Application,
                        clientId: session.ClientId,
                        targetId: session.SessionId.ToString("D"),
                        reasonCode: SecurityAuditReasonCode.InvalidSession),
                    cancellationToken).ConfigureAwait(false);

                return null;
            }

            await auditWriter.TryWriteAsync(
                location.Route,
                new SecurityAuditEvent(
                    SecurityAuditEventType.SessionAssuranceUpgraded,
                    SecurityAuditOutcome.Succeeded,
                    session.Subject.IdentityScopeId,
                    userId: session.Subject.UserId,
                    application: session.Application,
                    clientId: session.ClientId,
                    targetId: session.SessionId.ToString("D")),
                cancellationToken).ConfigureAwait(false);

            return ToContext(updated);
        }

        private async Task<AuthenticationDirectoryLocation?> ResolveAsync(
            AuthenticatedSessionContext session,
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
                return null;
            }

            return location.Route.Request.IdentityScopeId == session.Subject.IdentityScopeId &&
                location.Route.Request.Application == session.Application &&
                string.Equals(
                    location.AuthenticationContextKey,
                    session.AuthenticationContextKey,
                    StringComparison.Ordinal)
                ? location
                : null;
        }

        private static bool Matches(
            AuthenticatedSessionContext expected,
            AuthenticationSession? current) =>
            current is not null &&
            current.Subject == expected.Subject &&
            current.SessionId == expected.SessionId &&
            string.Equals(current.ClientId, expected.ClientId, StringComparison.Ordinal) &&
            current.Application == expected.Application &&
            string.Equals(
                current.AuthenticationContextKey,
                expected.AuthenticationContextKey,
                StringComparison.Ordinal);

        private static AuthenticatedSessionContext ToContext(AuthenticationSession session) =>
            new(
                session.Subject,
                session.SessionId,
                session.ClientId,
                session.Application,
                session.AuthenticationContextKey,
                session.CreatedAt,
                session.ExpiresAt,
                session.Assurance);
    }
}
