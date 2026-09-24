namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Internal atomic storage outcomes for recovery password replacement.</summary>
    internal enum RecoveryPasswordResetStoreResult
    {
        Succeeded = 1,
        InvalidCode = 2,
        NotFound = 3,
        AlreadyConsumed = 4,
        InactiveUser = 5
    }
}
