namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>One-time recovery-code material returned only when a set is generated or replaced.</summary>
    public sealed class RecoveryCodeSet
    {
        /// <summary>Gets the generic authenticator identifier for the active set.</summary>
        public Guid AuthenticatorId { get; }

        /// <summary>Gets the raw recovery codes. These values are not persisted and must be shown only once.</summary>
        public IReadOnlyList<string> Codes { get; }

        /// <summary>Gets whether an older active recovery-code set was revoked by this operation.</summary>
        public bool ReplacedExistingSet { get; }

        /// <summary>Initializes one generated recovery-code set.</summary>
        public RecoveryCodeSet(
            Guid authenticatorId,
            IEnumerable<string> codes,
            bool replacedExistingSet)
        {
            if (authenticatorId == Guid.Empty)
                throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            ArgumentNullException.ThrowIfNull(codes);

            var copy = codes.ToArray();
            if (copy.Length == 0)
                throw new ArgumentException("At least one recovery code is required.", nameof(codes));
            if (copy.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Recovery codes cannot be empty.", nameof(codes));
            if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
                throw new ArgumentException("Recovery codes must be unique.", nameof(codes));

            AuthenticatorId = authenticatorId;
            Codes = Array.AsReadOnly(copy);
            ReplacedExistingSet = replacedExistingSet;
        }
    }
}
