namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Internal assertion-verification outcome prior to durable authentication mutation.</summary>
    internal sealed class WebAuthnAuthenticationVerificationResult
    {
        public bool Succeeded { get; }
        public bool ClientDataValid { get; }
        public long SignCount { get; }
        public bool BackupEligible { get; }
        public bool BackupState { get; }

        private WebAuthnAuthenticationVerificationResult(
            bool succeeded,
            bool clientDataValid,
            long signCount,
            bool backupEligible,
            bool backupState)
        {
            Succeeded = succeeded;
            ClientDataValid = clientDataValid;
            SignCount = signCount;
            BackupEligible = backupEligible;
            BackupState = backupState;
        }

        internal static WebAuthnAuthenticationVerificationResult InvalidClientData() =>
            new(false, false, 0, false, false);

        internal static WebAuthnAuthenticationVerificationResult InvalidAssertion() =>
            new(false, true, 0, false, false);

        internal static WebAuthnAuthenticationVerificationResult Success(
            long signCount,
            bool backupEligible,
            bool backupState) =>
            new(true, true, signCount, backupEligible, backupState);
    }
}
