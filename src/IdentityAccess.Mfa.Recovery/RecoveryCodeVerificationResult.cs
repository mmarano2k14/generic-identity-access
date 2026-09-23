namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Result of verifying and consuming a recovery code.</summary>
    public enum RecoveryCodeVerificationResult
    {
        /// <summary>The proof was valid and was consumed exactly once.</summary>
        Succeeded = 1,
        /// <summary>The supplied code was malformed or did not match the selected active set.</summary>
        InvalidCode = 2,
        /// <summary>The authenticator does not exist for the requested user and scope.</summary>
        NotFound = 3,
        /// <summary>The recovery-code set is not active.</summary>
        NotActive = 4,
        /// <summary>The supplied code matched a code that had already been consumed.</summary>
        AlreadyConsumed = 5
    }
}
