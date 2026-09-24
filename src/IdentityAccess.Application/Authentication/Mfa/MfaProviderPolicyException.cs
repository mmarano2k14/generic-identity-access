namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Raised when a concrete factor provider is invoked outside the configured MFA policy.</summary>
    public sealed class MfaProviderPolicyException : InvalidOperationException
    {
        /// <summary>Gets the policy decision that rejected provider use.</summary>
        public MfaProviderPolicyDecision Decision { get; }

        /// <summary>Initializes a policy rejection.</summary>
        public MfaProviderPolicyException(MfaProviderPolicyDecision decision)
            : base("The authentication-factor provider is not permitted by the current MFA policy.")
        {
            if (decision == MfaProviderPolicyDecision.Allowed)
                throw new ArgumentOutOfRangeException(nameof(decision));

            Decision = decision;
        }
    }
}
