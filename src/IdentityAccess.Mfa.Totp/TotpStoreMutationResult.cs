namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Internal outcome of a concurrency-safe provider-store mutation.</summary>
    internal enum TotpStoreMutationResult
    {
        Succeeded = 1,
        NotFound = 2,
        InvalidState = 3,
        ReplayDetected = 4
    }
}
