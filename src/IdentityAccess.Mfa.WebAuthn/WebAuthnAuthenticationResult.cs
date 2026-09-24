namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Outcome of one WebAuthn authentication completion attempt.</summary>
    public enum WebAuthnAuthenticationResult
    {
        Succeeded = 1,
        NotFound = 2,
        Expired = 3,
        AlreadyUsed = 4,
        InvalidClientData = 5,
        InvalidAssertion = 6,
        ReplayDetected = 7,
        InvalidState = 8
    }
}
