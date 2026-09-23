namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Result of confirming a pending TOTP enrollment.</summary>
    public enum TotpConfirmationResult
    {
        /// <summary>The first valid code confirmed the enrollment.</summary>
        Confirmed = 1,
        /// <summary>The supplied proof was not valid in the accepted time window.</summary>
        InvalidCode = 2,
        /// <summary>The authenticator does not exist for the requested user and scope.</summary>
        NotFound = 3,
        /// <summary>The authenticator is no longer pending and cannot be confirmed.</summary>
        InvalidState = 4,
        /// <summary>The accepted time step had already been consumed.</summary>
        ReplayDetected = 5
    }
}
