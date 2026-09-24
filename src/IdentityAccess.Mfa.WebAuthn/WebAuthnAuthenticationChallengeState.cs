using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Persisted hash-only state for one WebAuthn authentication challenge.</summary>
    internal sealed class WebAuthnAuthenticationChallengeState
    {
        public ApplicationKey Application { get; }
        public byte[] ChallengeHash { get; }
        public DateTimeOffset ExpiresAt { get; }
        public DateTimeOffset? ConsumedAt { get; }

        public WebAuthnAuthenticationChallengeState(
            ApplicationKey application,
            byte[] challengeHash,
            DateTimeOffset expiresAt,
            DateTimeOffset? consumedAt)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(challengeHash);
            if (challengeHash.Length != 32) throw new ArgumentException("WebAuthn challenge hash must contain 32 bytes.", nameof(challengeHash));

            Application = application;
            ChallengeHash = challengeHash.ToArray();
            ExpiresAt = expiresAt;
            ConsumedAt = consumedAt;
        }
    }
}
