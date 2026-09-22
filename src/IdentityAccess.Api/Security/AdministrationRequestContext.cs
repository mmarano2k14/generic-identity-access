using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Represents the server-trusted authenticated identity attached to one administration
    /// request. Authorization targets such as tenant and resource scope are intentionally not
    /// part of this identity context.
    /// </summary>
    public sealed record AdministrationRequestContext
    {
        /// <summary>Gets the authenticated subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the validated session identifier.</summary>
        public Guid SessionId { get; }

        /// <summary>Gets the registered client identifier used to authenticate the request.</summary>
        public string ClientId { get; }

        /// <summary>Gets the server-registered application associated with the session.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the server-registered authentication context key.</summary>
        public string AuthenticationContextKey { get; }

        /// <summary>Gets the validated session expiration time.</summary>
        public DateTimeOffset ExpiresAt { get; }

        /// <summary>Initializes a trusted administration request context.</summary>
        public AdministrationRequestContext(
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
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
            AuthenticationContextKey = authenticationContextKey;
            ExpiresAt = expiresAt;
        }
    }
}
