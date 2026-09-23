namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Atomic PostgreSQL registration mutation outcome.</summary>
    internal enum WebAuthnRegistrationStoreResult
    {
        Succeeded = 1,
        NotFound = 2,
        Expired = 3,
        AlreadyUsed = 4,
        CredentialAlreadyRegistered = 5,
        InvalidState = 6
    }
}
