namespace IdentityAccess.Domain
{
    /// <summary>
    /// Generic authenticator metadata. Provider-specific secret or credential material is deliberately excluded.
    /// </summary>
    public sealed class UserAuthenticator
    {
        /// <summary>Gets the identity scope.</summary>
        public Guid IdentityScopeId { get; }

        /// <summary>Gets the authenticator identifier.</summary>
        public Guid AuthenticatorId { get; }

        /// <summary>Gets the user identifier.</summary>
        public Guid UserId { get; }

        /// <summary>Gets the provider key.</summary>
        public AuthenticationFactorProviderKey Provider { get; }

        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }

        /// <summary>Gets the lifecycle status.</summary>
        public UserAuthenticatorStatus Status { get; }

        /// <summary>Gets when enrollment was created.</summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>Gets when enrollment was confirmed.</summary>
        public DateTimeOffset? ConfirmedAt { get; }

        /// <summary>Gets when the authenticator was last used successfully.</summary>
        public DateTimeOffset? LastUsedAt { get; }

        /// <summary>Gets when the authenticator was revoked.</summary>
        public DateTimeOffset? RevokedAt { get; }

        /// <summary>Initializes generic authenticator metadata.</summary>
        public UserAuthenticator(
            Guid identityScopeId,
            Guid authenticatorId,
            Guid userId,
            AuthenticationFactorProviderKey provider,
            string displayName,
            UserAuthenticatorStatus status,
            DateTimeOffset createdAt,
            DateTimeOffset? confirmedAt,
            DateTimeOffset? lastUsedAt,
            DateTimeOffset? revokedAt)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            if (authenticatorId == Guid.Empty)
                throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            if (userId == Guid.Empty)
                throw new ArgumentException("User identifier is required.", nameof(userId));

            ArgumentNullException.ThrowIfNull(provider);

            IdentityScopeId = identityScopeId;
            AuthenticatorId = authenticatorId;
            UserId = userId;
            Provider = provider;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
            CreatedAt = createdAt;
            ConfirmedAt = confirmedAt;
            LastUsedAt = lastUsedAt;
            RevokedAt = revokedAt;

            if (confirmedAt is not null && confirmedAt < createdAt)
                throw new ArgumentOutOfRangeException(nameof(confirmedAt));
            if (lastUsedAt is not null && lastUsedAt < createdAt)
                throw new ArgumentOutOfRangeException(nameof(lastUsedAt));
            if (revokedAt is not null && revokedAt < createdAt)
                throw new ArgumentOutOfRangeException(nameof(revokedAt));

            if (status == UserAuthenticatorStatus.Pending && confirmedAt is not null)
                throw new ArgumentException("A pending authenticator cannot be confirmed.", nameof(confirmedAt));
            if (status == UserAuthenticatorStatus.Active && confirmedAt is null)
                throw new ArgumentException("An active authenticator requires confirmation.", nameof(confirmedAt));
            if (status == UserAuthenticatorStatus.Revoked && revokedAt is null)
                throw new ArgumentException("A revoked authenticator requires a revocation timestamp.", nameof(revokedAt));
            if (status != UserAuthenticatorStatus.Revoked && revokedAt is not null)
                throw new ArgumentException("Only revoked authenticators may have a revocation timestamp.", nameof(revokedAt));
        }
    }
}
