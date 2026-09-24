using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Persistent server session metadata. The raw opaque token is never stored in this object or in PostgreSQL.
    /// </summary>
    public sealed class AuthenticationSession
    {
        /// <summary>Gets the session identifier.</summary>
        public Guid SessionId { get; }
        /// <summary>Gets the subject.</summary>
        public SubjectReference Subject { get; }
        /// <summary>Gets the client identifier.</summary>
        public string ClientId { get; }
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the authentication context key.</summary>
        public string AuthenticationContextKey { get; }
        /// <summary>Gets the created at.</summary>
        public DateTimeOffset CreatedAt { get; }
        /// <summary>Gets the expires at.</summary>
        public DateTimeOffset ExpiresAt { get; }
        /// <summary>Gets the revoked at.</summary>
        public DateTimeOffset? RevokedAt { get; }
        /// <summary>Gets the current server-recorded authentication assurance.</summary>
        public AuthenticationAssurance Assurance { get; }

        /// <summary>Initializes a new instance of <see cref="AuthenticationSession"/>.</summary>
        public AuthenticationSession(
            Guid sessionId,
            SubjectReference subject,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            DateTimeOffset createdAt,
            DateTimeOffset expiresAt,
            DateTimeOffset? revokedAt = null,
            AuthenticationAssurance? assurance = null)
        {
            if (sessionId == Guid.Empty) throw new ArgumentException("A session identifier is required.", nameof(sessionId));
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationContextKey);
            if (expiresAt <= createdAt) throw new ArgumentException("Session expiry must be after creation.", nameof(expiresAt));

            var resolvedAssurance = assurance ?? AuthenticationAssurance.Password(createdAt);
            if (resolvedAssurance.VerifiedAt < createdAt || resolvedAssurance.VerifiedAt >= expiresAt)
            {
                throw new ArgumentException(
                    "Session assurance verification time must be within the session lifetime.",
                    nameof(assurance));
            }

            SessionId = sessionId;
            Subject = subject;
            ClientId = clientId;
            Application = application;
            AuthenticationContextKey = authenticationContextKey;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            RevokedAt = revokedAt;
            Assurance = resolvedAssurance;
        }
    }
}
