namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Describes whether one provider may participate in MFA for the current application policy.</summary>
    public enum MfaProviderPolicyDecision
    {
        /// <summary>The provider is explicitly allowed by the configured MFA policy.</summary>
        Allowed = 1,
        /// <summary>No MFA policy exists for the requested identity scope and application.</summary>
        PolicyNotConfigured = 2,
        /// <summary>MFA is disabled by policy.</summary>
        MfaDisabled = 3,
        /// <summary>The provider is not present in the policy allow-list.</summary>
        ProviderNotAllowed = 4
    }
}
