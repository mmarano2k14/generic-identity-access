namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Outcome of one WebAuthn registration completion attempt.</summary>
    public enum WebAuthnRegistrationResult
    {
        Registered = 1,
        NotFound = 2,
        Expired = 3,
        AlreadyUsed = 4,
        InvalidClientData = 5,
        InvalidAttestation = 6,
        CredentialAlreadyRegistered = 7,
        InvalidState = 8
    }
}
