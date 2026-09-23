namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Declares generic capabilities implemented by an authentication-factor provider.</summary>
    [Flags]
    public enum AuthenticationFactorProviderCapabilities
    {
        /// <summary>No operational capability is declared.</summary>
        None = 0,
        /// <summary>The provider can enroll user authenticators.</summary>
        Enrollment = 1,
        /// <summary>The provider can verify authentication-factor proofs.</summary>
        Verification = 2,
        /// <summary>The provider can provide account-recovery proofs.</summary>
        Recovery = 4
    }
}
