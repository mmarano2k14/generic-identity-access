namespace IdentityAccess.Mfa.Totp
{
    /// <summary>One-time enrollment material returned to the trusted caller.</summary>
    public sealed class TotpEnrollment
    {
        /// <summary>Gets the generic authenticator identifier.</summary>
        public Guid AuthenticatorId { get; }

        /// <summary>Gets the Base32 secret. The caller must treat this value as sensitive and show it only during enrollment.</summary>
        public string Secret { get; }

        /// <summary>Gets the standard authenticator-app provisioning URI.</summary>
        public string ProvisioningUri { get; }

        /// <summary>Gets the HMAC algorithm name.</summary>
        public string Algorithm { get; }

        /// <summary>Gets the number of generated decimal digits.</summary>
        public int Digits { get; }

        /// <summary>Gets the TOTP period in seconds.</summary>
        public int PeriodSeconds { get; }

        /// <summary>Initializes one enrollment response.</summary>
        public TotpEnrollment(
            Guid authenticatorId,
            string secret,
            string provisioningUri,
            string algorithm,
            int digits,
            int periodSeconds)
        {
            if (authenticatorId == Guid.Empty)
                throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            ArgumentException.ThrowIfNullOrWhiteSpace(secret);
            ArgumentException.ThrowIfNullOrWhiteSpace(provisioningUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
            if (digits is < 6 or > 8) throw new ArgumentOutOfRangeException(nameof(digits));
            if (periodSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(periodSeconds));

            AuthenticatorId = authenticatorId;
            Secret = secret;
            ProvisioningUri = provisioningUri;
            Algorithm = algorithm;
            Digits = digits;
            PeriodSeconds = periodSeconds;
        }
    }
}
