using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Represents trusted access-token identity and provenance after signature, lifetime, issuer,
    /// audience, and registered-client validation. The raw encoded token is deliberately excluded.
    /// </summary>
    public sealed record ValidatedOidcAccessToken
    {
        /// <summary>Gets the stable logical subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the source local-session identifier carried by the token.</summary>
        public Guid SessionId { get; }

        /// <summary>Gets the registered public client identifier.</summary>
        public string ClientId { get; }

        /// <summary>Gets the server-registered application bound to the client.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the server-registered authentication-context key bound to the client.</summary>
        public string AuthenticationContextKey { get; }

        /// <summary>Gets the canonical granted scope string.</summary>
        public string Scope { get; }

        /// <summary>Gets the JWT identifier.</summary>
        public Guid TokenId { get; }

        /// <summary>Gets the token issuance time.</summary>
        public DateTimeOffset IssuedAt { get; }

        /// <summary>Gets the token expiry time.</summary>
        public DateTimeOffset ExpiresAt { get; }

        /// <summary>Initializes a validated access-token context.</summary>
        public ValidatedOidcAccessToken(
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            string scope,
            Guid tokenId,
            DateTimeOffset issuedAt,
            DateTimeOffset expiresAt)
        {
            ArgumentNullException.ThrowIfNull(subject);

            if (sessionId == Guid.Empty)
                throw new ArgumentException("A session identifier is required.", nameof(sessionId));

            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationContextKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(scope);

            if (tokenId == Guid.Empty)
                throw new ArgumentException("A token identifier is required.", nameof(tokenId));

            if (expiresAt <= issuedAt)
                throw new ArgumentException("Access-token expiry must be after issuance.", nameof(expiresAt));

            Subject = subject;
            SessionId = sessionId;
            ClientId = clientId;
            Application = application;
            AuthenticationContextKey = authenticationContextKey;
            Scope = scope;
            TokenId = tokenId;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
        }
    }
}
