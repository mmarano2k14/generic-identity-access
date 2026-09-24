using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Represents server-validated session identity, provenance, and assurance. The raw opaque
    /// session token is deliberately excluded.
    /// </summary>
    public sealed record AuthenticatedSessionContext
    {
        /// <summary>Gets the authenticated subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the validated session identifier.</summary>
        public Guid SessionId { get; }

        /// <summary>Gets the registered authentication client identifier.</summary>
        public string ClientId { get; }

        /// <summary>Gets the server-registered application associated with the session.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the server-registered authentication context key.</summary>
        public string AuthenticationContextKey { get; }

        /// <summary>Gets the original local password-authentication time.</summary>
        public DateTimeOffset AuthenticatedAt { get; }

        /// <summary>Gets the session expiration time.</summary>
        public DateTimeOffset ExpiresAt { get; }

        /// <summary>Gets the current server-recorded authentication assurance.</summary>
        public AuthenticationAssurance Assurance { get; }

        /// <summary>Initializes a server-validated session context.</summary>
        public AuthenticatedSessionContext(
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            DateTimeOffset authenticatedAt,
            DateTimeOffset expiresAt,
            AuthenticationAssurance? assurance = null)
        {
            ArgumentNullException.ThrowIfNull(subject);

            if (sessionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A session identifier is required.",
                    nameof(sessionId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationContextKey);

            if (authenticatedAt >= expiresAt)
            {
                throw new ArgumentException(
                    "Authentication time must be before session expiry.",
                    nameof(authenticatedAt));
            }

            var resolvedAssurance = assurance ?? AuthenticationAssurance.Password(authenticatedAt);
            if (resolvedAssurance.VerifiedAt < authenticatedAt || resolvedAssurance.VerifiedAt >= expiresAt)
            {
                throw new ArgumentException(
                    "Session assurance verification time must be within the session lifetime.",
                    nameof(assurance));
            }

            Subject = subject;
            SessionId = sessionId;
            ClientId = clientId;
            Application = application;
            AuthenticationContextKey = authenticationContextKey;
            AuthenticatedAt = authenticatedAt;
            ExpiresAt = expiresAt;
            Assurance = resolvedAssurance;
        }
    }
}
