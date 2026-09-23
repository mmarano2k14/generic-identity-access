using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Provider-owned persisted state required to validate one registration response.</summary>
    internal sealed class WebAuthnPendingRegistrationState
    {
        public UserAuthenticatorStatus Status { get; }
        public ApplicationKey Application { get; }
        public byte[] ChallengeHash { get; }
        public DateTimeOffset ExpiresAt { get; }
        public DateTimeOffset? ConsumedAt { get; }

        public WebAuthnPendingRegistrationState(
            UserAuthenticatorStatus status,
            ApplicationKey application,
            byte[] challengeHash,
            DateTimeOffset expiresAt,
            DateTimeOffset? consumedAt)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(challengeHash);
            if (challengeHash.Length != 32)
                throw new ArgumentException("WebAuthn challenge hash must contain 32 bytes.", nameof(challengeHash));

            Status = status;
            Application = application;
            ChallengeHash = challengeHash.ToArray();
            ExpiresAt = expiresAt;
            ConsumedAt = consumedAt;
        }
    }
}
