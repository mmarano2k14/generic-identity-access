namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Raised when an administrative mutation would violate a required MFA policy.</summary>
    public sealed class MfaPolicyComplianceException : InvalidOperationException
    {
        /// <summary>Initializes the policy-compliance failure.</summary>
        public MfaPolicyComplianceException()
            : base("The operation would leave the user without an active verification factor required by policy.")
        {
        }
    }
}
