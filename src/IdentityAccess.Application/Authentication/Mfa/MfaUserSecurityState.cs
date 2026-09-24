using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Provider-neutral effective MFA state for one user in one application.</summary>
    public sealed record MfaUserSecurityState
    {
        /// <summary>Gets whether an application MFA policy is configured.</summary>
        public bool PolicyConfigured { get; }

        /// <summary>Gets the configured policy mode when a policy exists.</summary>
        public MfaPolicyMode? PolicyMode { get; }

        /// <summary>Gets whether the current policy requires an additional factor.</summary>
        public bool MfaRequired { get; }

        /// <summary>Gets whether at least one active allowed verification-capable factor exists.</summary>
        public bool HasActiveVerificationFactor { get; }

        /// <summary>Gets whether at least one active allowed non-recovery verification factor exists.</summary>
        public bool HasActivePrimaryFactor { get; }

        /// <summary>Gets whether at least one active allowed recovery-capable factor exists.</summary>
        public bool HasActiveRecoveryFactor { get; }

        /// <summary>Gets whether the currently enrolled factor set satisfies the configured policy.</summary>
        public bool SatisfiesCurrentPolicy { get; }

        /// <summary>Gets active allowed verification provider keys.</summary>
        public IReadOnlyList<AuthenticationFactorProviderKey> ActiveVerificationProviders { get; }

        /// <summary>Gets active allowed primary provider keys.</summary>
        public IReadOnlyList<AuthenticationFactorProviderKey> ActivePrimaryProviders { get; }

        /// <summary>Gets active allowed recovery provider keys.</summary>
        public IReadOnlyList<AuthenticationFactorProviderKey> ActiveRecoveryProviders { get; }

        /// <summary>Initializes effective user MFA state from normalized provider sets.</summary>
        public MfaUserSecurityState(
            bool policyConfigured,
            MfaPolicyMode? policyMode,
            IEnumerable<AuthenticationFactorProviderKey> activeVerificationProviders,
            IEnumerable<AuthenticationFactorProviderKey> activePrimaryProviders,
            IEnumerable<AuthenticationFactorProviderKey> activeRecoveryProviders)
        {
            if (policyConfigured != policyMode.HasValue)
                throw new ArgumentException("Policy configuration and policy mode must agree.", nameof(policyMode));

            PolicyConfigured = policyConfigured;
            PolicyMode = policyMode;
            ActiveVerificationProviders = Normalize(activeVerificationProviders);
            ActivePrimaryProviders = Normalize(activePrimaryProviders);
            ActiveRecoveryProviders = Normalize(activeRecoveryProviders);

            var verification = ActiveVerificationProviders.ToHashSet();
            if (ActivePrimaryProviders.Any(provider => !verification.Contains(provider)) ||
                ActiveRecoveryProviders.Any(provider => !verification.Contains(provider)))
            {
                throw new ArgumentException("Primary and recovery providers must also be verification providers.");
            }

            if (ActivePrimaryProviders.Intersect(ActiveRecoveryProviders).Any())
                throw new ArgumentException("A provider cannot be both primary and recovery in one effective state.");

            MfaRequired = policyMode == MfaPolicyMode.Required;
            HasActiveVerificationFactor = ActiveVerificationProviders.Count > 0;
            HasActivePrimaryFactor = ActivePrimaryProviders.Count > 0;
            HasActiveRecoveryFactor = ActiveRecoveryProviders.Count > 0;
            SatisfiesCurrentPolicy = !MfaRequired || HasActivePrimaryFactor;
        }

        private static IReadOnlyList<AuthenticationFactorProviderKey> Normalize(
            IEnumerable<AuthenticationFactorProviderKey> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);
            return providers
                .Select(provider => provider ?? throw new ArgumentException(
                    "Provider keys cannot contain null values.",
                    nameof(providers)))
                .Distinct()
                .OrderBy(provider => provider.Value, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
