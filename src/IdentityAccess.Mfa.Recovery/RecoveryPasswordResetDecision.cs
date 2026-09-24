namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Defines stable outcomes for recovery-code-backed password replacement.</summary>
    public enum RecoveryPasswordResetDecision
    {
        /// <summary>The recovery proof was consumed and the credential was replaced.</summary>
        Succeeded = 1,
        /// <summary>The client is not a registered authentication client.</summary>
        UnknownClient = 2,
        /// <summary>The requested identity directory could not be resolved.</summary>
        Unavailable = 3,
        /// <summary>The recovery proof was rejected without disclosing account existence.</summary>
        Rejected = 4,
        /// <summary>The replacement password matches the current password.</summary>
        PasswordReuseRejected = 5
    }
}
