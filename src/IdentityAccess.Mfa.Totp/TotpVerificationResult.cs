namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Result of verifying a TOTP proof for an active authenticator.</summary>
    public enum TotpVerificationResult
    {
        /// <summary>The proof was valid and its time step was consumed.</summary>
        Succeeded = 1,
        /// <summary>The supplied proof was not valid in the accepted time window.</summary>
        InvalidCode = 2,
        /// <summary>The authenticator does not exist for the requested user and scope.</summary>
        NotFound = 3,
        /// <summary>The authenticator is not active.</summary>
        NotActive = 4,
        /// <summary>The accepted time step had already been consumed.</summary>
        ReplayDetected = 5
    }
}
