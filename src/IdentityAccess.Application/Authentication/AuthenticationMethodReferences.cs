using System.Text.RegularExpressions;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines stable authentication-method references used in session and OIDC assurance metadata.</summary>
    public static partial class AuthenticationMethodReferences
    {
        /// <summary>RFC 8176 password authentication.</summary>
        public const string Password = "pwd";

        /// <summary>RFC 8176 multi-factor authentication marker.</summary>
        public const string MultiFactor = "mfa";

        /// <summary>RFC 8176 one-time-password authentication, used for TOTP.</summary>
        public const string OneTimePassword = "otp";

        /// <summary>Provider-neutral proof-of-possession marker used for WebAuthn assertions.</summary>
        public const string ProofOfPossession = "pop";

        /// <summary>Private recovery authentication method reference owned by this service.</summary>
        public const string Recovery = "recovery";

        /// <summary>Validates a provider/factor method reference before it enters durable assurance state.</summary>
        public static string ValidateFactor(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var normalized = value.Trim();

            if (!MethodSyntax().IsMatch(normalized) ||
                string.Equals(normalized, Password, StringComparison.Ordinal) ||
                string.Equals(normalized, MultiFactor, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Authentication factor method reference is invalid.",
                    nameof(value));
            }

            return normalized;
        }

        [GeneratedRegex("^[a-z][a-z0-9._:-]{0,63}$", RegexOptions.CultureInvariant)]
        private static partial Regex MethodSyntax();
    }
}
