namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Internal protocol-verification outcome prior to durable registration mutation.</summary>
    internal sealed class WebAuthnRegistrationVerificationResult
    {
        public bool Succeeded { get; }
        public bool ClientDataValid { get; }
        public WebAuthnCredentialMaterial? Credential { get; }

        private WebAuthnRegistrationVerificationResult(
            bool succeeded,
            bool clientDataValid,
            WebAuthnCredentialMaterial? credential)
        {
            Succeeded = succeeded;
            ClientDataValid = clientDataValid;
            Credential = credential;
        }

        internal static WebAuthnRegistrationVerificationResult InvalidClientData() =>
            new(false, false, null);

        internal static WebAuthnRegistrationVerificationResult InvalidAttestation() =>
            new(false, true, null);

        internal static WebAuthnRegistrationVerificationResult Success(WebAuthnCredentialMaterial credential) =>
            new(true, true, credential);
    }
}
