namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Immutable authentication assurance recorded against one local session. Method references
    /// describe how the current assurance was established; they never contain credentials or proofs.
    /// </summary>
    public sealed class AuthenticationAssurance
    {
        private readonly IReadOnlyList<string> methods;

        /// <summary>Gets the assurance level.</summary>
        public AuthenticationAssuranceLevel Level { get; }

        /// <summary>Gets the stable method references used to establish the current assurance.</summary>
        public IReadOnlyList<string> Methods => methods;

        /// <summary>Gets when the current assurance level was most recently established.</summary>
        public DateTimeOffset VerifiedAt { get; }

        /// <summary>Gets the service-defined OIDC authentication-context-class value.</summary>
        public string Acr => Level == AuthenticationAssuranceLevel.MultiFactor
            ? "urn:generic-identity-access:acr:mfa"
            : "urn:generic-identity-access:acr:password";

        /// <summary>Gets whether the session currently carries multi-factor assurance.</summary>
        public bool IsMultiFactor => Level == AuthenticationAssuranceLevel.MultiFactor;

        /// <summary>Initializes validated persisted assurance.</summary>
        public AuthenticationAssurance(
            AuthenticationAssuranceLevel level,
            IEnumerable<string> methods,
            DateTimeOffset verifiedAt)
        {
            if (!Enum.IsDefined(level))
                throw new ArgumentOutOfRangeException(nameof(level));

            ArgumentNullException.ThrowIfNull(methods);

            var normalized = methods
                .Select(value =>
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    var trimmed = value.Trim();
                    if (trimmed.Length > 64 || trimmed.Any(char.IsControl))
                        throw new ArgumentException("Authentication method reference is invalid.", nameof(methods));
                    return trimmed;
                })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            if (normalized.Length == 0 ||
                !normalized.Contains(AuthenticationMethodReferences.Password, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    "Authentication assurance must retain the password method reference.",
                    nameof(methods));
            }

            if (level == AuthenticationAssuranceLevel.PasswordOnly)
            {
                if (normalized.Length != 1 ||
                    !string.Equals(normalized[0], AuthenticationMethodReferences.Password, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Password-only assurance may contain only the password method reference.",
                        nameof(methods));
                }
            }
            else
            {
                if (!normalized.Contains(AuthenticationMethodReferences.MultiFactor, StringComparer.Ordinal) ||
                    normalized.All(value =>
                        string.Equals(value, AuthenticationMethodReferences.Password, StringComparison.Ordinal) ||
                        string.Equals(value, AuthenticationMethodReferences.MultiFactor, StringComparison.Ordinal)))
                {
                    throw new ArgumentException(
                        "Multi-factor assurance requires the mfa marker and a concrete additional factor method.",
                        nameof(methods));
                }
            }

            Level = level;
            this.methods = Array.AsReadOnly(normalized);
            VerifiedAt = verifiedAt;
        }

        /// <summary>Creates the initial password-only assurance for a newly authenticated session.</summary>
        public static AuthenticationAssurance Password(DateTimeOffset verifiedAt) =>
            new(
                AuthenticationAssuranceLevel.PasswordOnly,
                [AuthenticationMethodReferences.Password],
                verifiedAt);

        /// <summary>Returns upgraded assurance after a concrete additional factor succeeds.</summary>
        public AuthenticationAssurance WithFactor(
            string factorMethodReference,
            DateTimeOffset verifiedAt)
        {
            var factor = AuthenticationMethodReferences.ValidateFactor(factorMethodReference);

            return new AuthenticationAssurance(
                AuthenticationAssuranceLevel.MultiFactor,
                methods
                    .Append(AuthenticationMethodReferences.MultiFactor)
                    .Append(factor),
                verifiedAt);
        }

        /// <summary>Determines whether this assurance is multi-factor and within the requested age.</summary>
        public bool IsRecentMultiFactor(DateTimeOffset now, TimeSpan maximumAge)
        {
            if (maximumAge <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maximumAge));

            return IsMultiFactor &&
                VerifiedAt <= now &&
                now - VerifiedAt <= maximumAge;
        }
    }
}
