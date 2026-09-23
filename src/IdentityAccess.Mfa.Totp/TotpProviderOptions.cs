namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Validated server-side settings for the built-in TOTP provider.</summary>
    internal sealed class TotpProviderOptions
    {
        internal const string AlgorithmName = "SHA1";
        internal const int Digits = 6;
        internal const int PeriodSeconds = 30;
        internal const int SecretLengthBytes = 20;

        public string Issuer { get; }
        public int AllowedClockSkewSteps { get; }

        public TotpProviderOptions(string issuer, int allowedClockSkewSteps)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
            if (issuer.Trim().Length > 128)
                throw new ArgumentException("TOTP issuer must not exceed 128 characters.", nameof(issuer));
            if (allowedClockSkewSteps is < 0 or > 2)
                throw new ArgumentOutOfRangeException(nameof(allowedClockSkewSteps));

            Issuer = issuer.Trim();
            AllowedClockSkewSteps = allowedClockSkewSteps;
        }
    }
}
