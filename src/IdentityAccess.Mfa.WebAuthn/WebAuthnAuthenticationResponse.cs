namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Base64url-encoded browser response for one WebAuthn authentication ceremony.</summary>
    public sealed class WebAuthnAuthenticationResponse
    {
        public string CredentialId { get; }
        public string ClientDataJson { get; }
        public string AuthenticatorData { get; }
        public string Signature { get; }
        public string? UserHandle { get; }

        public WebAuthnAuthenticationResponse(
            string credentialId,
            string clientDataJson,
            string authenticatorData,
            string signature,
            string? userHandle = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientDataJson);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticatorData);
            ArgumentException.ThrowIfNullOrWhiteSpace(signature);

            CredentialId = credentialId.Trim();
            ClientDataJson = clientDataJson.Trim();
            AuthenticatorData = authenticatorData.Trim();
            Signature = signature.Trim();
            UserHandle = string.IsNullOrWhiteSpace(userHandle) ? null : userHandle.Trim();
        }
    }
}
