namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Internal mutation outcomes returned by provider-owned storage.</summary>
    internal enum RecoveryCodeStoreMutationResult
    {
        Succeeded = 1,
        InvalidCode = 2,
        NotFound = 3,
        NotActive = 4,
        AlreadyConsumed = 5
    }
}
