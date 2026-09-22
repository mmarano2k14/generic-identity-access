using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Represents the durable server-side metadata bound to one opaque authorization code. The raw
    /// authorization code and PKCE verifier are deliberately absent.
    /// </summary>
    public sealed record OidcAuthorizationCodeGrant
    {
        /// <summary>Gets the server-generated code record identifier.</summary>
        public Guid CodeId { get; }

        /// <summary>Gets the authenticated subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the originating local session identifier.</summary>
        public Guid SessionId { get; }

        /// <summary>Gets the registered public client identifier.</summary>
        public string ClientId { get; }

        /// <summary>Gets the registered application.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the registered authentication-context key.</summary>
        public string AuthenticationContextKey { get; }

        /// <summary>Gets the exact registered redirect URI.</summary>
        public string RedirectUri { get; }

        /// <summary>Gets the canonical granted scope string.</summary>
        public string Scope { get; }

        /// <summary>Gets the S256 PKCE code challenge.</summary>
        public string CodeChallenge { get; }

        /// <summary>Gets the OIDC nonce.</summary>
        public string Nonce { get; }

        /// <summary>Gets the original local authentication time.</summary>
        public DateTimeOffset AuthenticatedAt { get; }

        /// <summary>Gets when the authorization code was issued.</summary>
        public DateTimeOffset IssuedAt { get; }

        /// <summary>Gets when the authorization code expires.</summary>
        public DateTimeOffset ExpiresAt { get; }

        /// <summary>Gets when the authorization code was consumed.</summary>
        public DateTimeOffset? ConsumedAt { get; }

        /// <summary>Initializes an authorization-code grant.</summary>
        public OidcAuthorizationCodeGrant(
            Guid codeId,
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            string redirectUri,
            string scope,
            string codeChallenge,
            string nonce,
            DateTimeOffset authenticatedAt,
            DateTimeOffset issuedAt,
            DateTimeOffset expiresAt,
            DateTimeOffset? consumedAt = null)
        {
            if (codeId == Guid.Empty)
                throw new ArgumentException("Code id must not be empty.", nameof(codeId));

            ArgumentNullException.ThrowIfNull(subject);

            if (sessionId == Guid.Empty)
                throw new ArgumentException("Session id must not be empty.", nameof(sessionId));

            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationContextKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(scope);
            ArgumentException.ThrowIfNullOrWhiteSpace(codeChallenge);
            ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

            if (expiresAt <= issuedAt)
                throw new ArgumentException("Authorization code expiry must be after issuance.", nameof(expiresAt));

            if (authenticatedAt > issuedAt)
                throw new ArgumentException("Authentication time must not be after code issuance.", nameof(authenticatedAt));

            if (consumedAt is not null && consumedAt.Value < issuedAt)
                throw new ArgumentException("Code consumption time must not be before issuance.", nameof(consumedAt));

            CodeId = codeId;
            Subject = subject;
            SessionId = sessionId;
            ClientId = clientId;
            Application = application;
            AuthenticationContextKey = authenticationContextKey;
            RedirectUri = redirectUri;
            Scope = scope;
            CodeChallenge = codeChallenge;
            Nonce = nonce;
            AuthenticatedAt = authenticatedAt;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
            ConsumedAt = consumedAt;
        }
    }
}
