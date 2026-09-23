namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Browser-facing public-key credential creation options for one registration ceremony.</summary>
    public sealed class WebAuthnRegistrationOptions
    {
        public Guid AuthenticatorId { get; }
        public string Challenge { get; }
        public string RelyingPartyId { get; }
        public string RelyingPartyName { get; }
        public string UserId { get; }
        public string UserName { get; }
        public string UserDisplayName { get; }
        public int TimeoutMilliseconds { get; }
        public IReadOnlyList<int> PublicKeyCredentialAlgorithms { get; }
        public IReadOnlyList<string> ExcludeCredentialIds { get; }
        public string Attestation { get; } = "none";
        public string ResidentKey { get; } = "required";
        public string UserVerification { get; } = "required";

        public WebAuthnRegistrationOptions(
            Guid authenticatorId,
            string challenge,
            string relyingPartyId,
            string relyingPartyName,
            string userId,
            string userName,
            string userDisplayName,
            int timeoutMilliseconds,
            IEnumerable<int> publicKeyCredentialAlgorithms,
            IEnumerable<string> excludeCredentialIds)
        {
            if (authenticatorId == Guid.Empty)
                throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            ArgumentException.ThrowIfNullOrWhiteSpace(challenge);
            ArgumentException.ThrowIfNullOrWhiteSpace(relyingPartyId);
            ArgumentException.ThrowIfNullOrWhiteSpace(relyingPartyName);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(userName);
            ArgumentException.ThrowIfNullOrWhiteSpace(userDisplayName);
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
            ArgumentNullException.ThrowIfNull(publicKeyCredentialAlgorithms);
            ArgumentNullException.ThrowIfNull(excludeCredentialIds);

            var algorithms = publicKeyCredentialAlgorithms.Distinct().ToArray();
            if (algorithms.Length == 0)
                throw new ArgumentException("At least one public-key algorithm is required.", nameof(publicKeyCredentialAlgorithms));

            var excludes = excludeCredentialIds.ToArray();
            if (excludes.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Excluded credential identifiers cannot be empty.", nameof(excludeCredentialIds));

            AuthenticatorId = authenticatorId;
            Challenge = challenge;
            RelyingPartyId = relyingPartyId;
            RelyingPartyName = relyingPartyName;
            UserId = userId;
            UserName = userName.Trim();
            UserDisplayName = userDisplayName.Trim();
            TimeoutMilliseconds = timeoutMilliseconds;
            PublicKeyCredentialAlgorithms = Array.AsReadOnly(algorithms);
            ExcludeCredentialIds = Array.AsReadOnly(excludes);
        }
    }
}
