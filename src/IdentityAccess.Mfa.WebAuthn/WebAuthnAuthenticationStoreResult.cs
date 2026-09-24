namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Atomic persistence outcome after a cryptographically valid WebAuthn assertion.</summary>
    internal enum WebAuthnAuthenticationStoreResult
    {
        Succeeded = 1,
        NotFound = 2,
        Expired = 3,
        AlreadyUsed = 4,
        InvalidState = 5,
        ReplayDetected = 6
    }
}
