namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Browser-facing public-key credential request options for one authentication ceremony.</summary>
    public sealed class WebAuthnAuthenticationOptions
    {
        public Guid ChallengeId { get; }
        public string Challenge { get; }
        public string RelyingPartyId { get; }
        public int TimeoutMilliseconds { get; }
        public IReadOnlyList<string> AllowCredentialIds { get; }
        public string UserVerification { get; } = "required";

        public WebAuthnAuthenticationOptions(
            Guid challengeId,
            string challenge,
            string relyingPartyId,
            int timeoutMilliseconds,
            IEnumerable<string> allowCredentialIds)
        {
            if (challengeId == Guid.Empty)
                throw new ArgumentException("Challenge identifier is required.", nameof(challengeId));
            ArgumentException.ThrowIfNullOrWhiteSpace(challenge);
            ArgumentException.ThrowIfNullOrWhiteSpace(relyingPartyId);
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
            ArgumentNullException.ThrowIfNull(allowCredentialIds);

            var credentials = allowCredentialIds.ToArray();
            if (credentials.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Allowed credential identifiers cannot be empty.", nameof(allowCredentialIds));

            ChallengeId = challengeId;
            Challenge = challenge;
            RelyingPartyId = relyingPartyId;
            TimeoutMilliseconds = timeoutMilliseconds;
            AllowCredentialIds = Array.AsReadOnly(credentials);
        }
    }
}
