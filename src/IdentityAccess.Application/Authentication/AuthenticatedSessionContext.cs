using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Represents server-validated session identity and provenance. The raw opaque session token is
    /// deliberately excluded.
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

        /// <summary>Gets the original local authentication time.</summary>
        public DateTimeOffset AuthenticatedAt { get; }

        /// <summary>Gets the session expiration time.</summary>
        public DateTimeOffset ExpiresAt { get; }

        /// <summary>Initializes a server-validated session context.</summary>
        public AuthenticatedSessionContext(
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            DateTimeOffset authenticatedAt,
            DateTimeOffset expiresAt)
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

            Subject = subject;
            SessionId = sessionId;
            ClientId = clientId;
            Application = application;
            if (authenticatedAt >= expiresAt)
            {
                throw new ArgumentException(
                    "Authentication time must be before session expiry.",
                    nameof(authenticatedAt));
            }

            AuthenticationContextKey = authenticationContextKey;
            AuthenticatedAt = authenticatedAt;
            ExpiresAt = expiresAt;
        }
    }
}
