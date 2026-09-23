namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Base64url-encoded browser response for one WebAuthn credential creation ceremony.</summary>
    public sealed class WebAuthnRegistrationResponse
    {
        public string CredentialId { get; }
        public string ClientDataJson { get; }
        public string AttestationObject { get; }

        public WebAuthnRegistrationResponse(
            string credentialId,
            string clientDataJson,
            string attestationObject)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientDataJson);
            ArgumentException.ThrowIfNullOrWhiteSpace(attestationObject);

            CredentialId = credentialId.Trim();
            ClientDataJson = clientDataJson.Trim();
            AttestationObject = attestationObject.Trim();
        }
    }
}
