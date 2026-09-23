using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Provider-owned durable state joined with the generic authenticator lifecycle.</summary>
    internal sealed class TotpAuthenticatorState
    {
        public UserAuthenticatorStatus Status { get; }
        public byte[] ProtectedSecret { get; }
        public string Algorithm { get; }
        public int Digits { get; }
        public int PeriodSeconds { get; }
        public long? LastAcceptedTimeStep { get; }

        public TotpAuthenticatorState(
            UserAuthenticatorStatus status,
            byte[] protectedSecret,
            string algorithm,
            int digits,
            int periodSeconds,
            long? lastAcceptedTimeStep)
        {
            ArgumentNullException.ThrowIfNull(protectedSecret);
            ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
            if (protectedSecret.Length == 0) throw new ArgumentException("Protected secret is required.", nameof(protectedSecret));
            if (digits is < 6 or > 8) throw new ArgumentOutOfRangeException(nameof(digits));
            if (periodSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(periodSeconds));
            if (lastAcceptedTimeStep < 0) throw new ArgumentOutOfRangeException(nameof(lastAcceptedTimeStep));

            Status = status;
            ProtectedSecret = protectedSecret;
            Algorithm = algorithm;
            Digits = digits;
            PeriodSeconds = periodSeconds;
            LastAcceptedTimeStep = lastAcceptedTimeStep;
        }
    }
}
