using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Represents durable non-secret metadata for one refresh token in a rotation family. Raw
    /// refresh-token values and persistence hashes are deliberately absent.
    /// </summary>
    public sealed record OidcRefreshTokenGrant
    {
        /// <summary>Gets the immutable refresh-token family identifier.</summary>
        public Guid FamilyId { get; }

        /// <summary>Gets this refresh-token record identifier.</summary>
        public Guid TokenId { get; }

        /// <summary>Gets the token identifier from which this token was rotated, when applicable.</summary>
        public Guid? ParentTokenId { get; }

        /// <summary>Gets the zero-based sequence number within the family.</summary>
        public long SequenceNumber { get; }

        /// <summary>Gets the authenticated subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the source local session identifier.</summary>
        public Guid SessionId { get; }

        /// <summary>Gets the registered public client identifier.</summary>
        public string ClientId { get; }

        /// <summary>Gets the registered application.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the registered authentication-context key.</summary>
        public string AuthenticationContextKey { get; }

        /// <summary>Gets the canonical granted scope string.</summary>
        public string Scope { get; }

        /// <summary>Gets the original local authentication time.</summary>
        public DateTimeOffset AuthenticatedAt { get; }

        /// <summary>Gets the authentication assurance pinned to this refresh-token family member.</summary>
        public AuthenticationAssurance Assurance { get; }

        /// <summary>Gets when this refresh token was issued.</summary>
        public DateTimeOffset IssuedAt { get; }

        /// <summary>Gets the absolute refresh-token family expiry.</summary>
        public DateTimeOffset ExpiresAt { get; }

        /// <summary>Initializes one refresh-token family member.</summary>
        public OidcRefreshTokenGrant(
            Guid familyId,
            Guid tokenId,
            Guid? parentTokenId,
            long sequenceNumber,
            SubjectReference subject,
            Guid sessionId,
            string clientId,
            ApplicationKey application,
            string authenticationContextKey,
            string scope,
            DateTimeOffset authenticatedAt,
            DateTimeOffset issuedAt,
            DateTimeOffset expiresAt,
            AuthenticationAssurance? assurance = null)
        {
            if (familyId == Guid.Empty)
                throw new ArgumentException("Family id must not be empty.", nameof(familyId));

            if (tokenId == Guid.Empty)
                throw new ArgumentException("Token id must not be empty.", nameof(tokenId));

            if (parentTokenId == Guid.Empty)
                throw new ArgumentException("Parent token id must not be empty when supplied.", nameof(parentTokenId));

            if (sequenceNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(sequenceNumber));

            ArgumentNullException.ThrowIfNull(subject);

            if (sessionId == Guid.Empty)
                throw new ArgumentException("Session id must not be empty.", nameof(sessionId));

            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationContextKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(scope);

            if (authenticatedAt > issuedAt)
                throw new ArgumentException("Authentication time must not be after token issuance.", nameof(authenticatedAt));

            if (expiresAt <= issuedAt)
                throw new ArgumentException("Refresh-token expiry must be after issuance.", nameof(expiresAt));

            if (sequenceNumber == 0 && parentTokenId is not null)
                throw new ArgumentException("Initial refresh tokens must not have a parent token.", nameof(parentTokenId));

            if (sequenceNumber > 0 && parentTokenId is null)
                throw new ArgumentException("Rotated refresh tokens require a parent token.", nameof(parentTokenId));

            FamilyId = familyId;
            TokenId = tokenId;
            ParentTokenId = parentTokenId;
            SequenceNumber = sequenceNumber;
            Subject = subject;
            SessionId = sessionId;
            ClientId = clientId;
            Application = application;
            AuthenticationContextKey = authenticationContextKey;
            var resolvedAssurance = assurance ?? AuthenticationAssurance.Password(authenticatedAt);
            if (resolvedAssurance.VerifiedAt != authenticatedAt)
                throw new ArgumentException("Refresh-token assurance time must match authenticated_at.", nameof(assurance));

            Scope = scope;
            AuthenticatedAt = authenticatedAt;
            Assurance = resolvedAssurance;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
        }
    }
}
