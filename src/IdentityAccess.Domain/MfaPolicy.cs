namespace IdentityAccess.Domain
{
    /// <summary>Generic MFA policy for one identity scope and application.</summary>
    public sealed class MfaPolicy
    {
        private readonly IReadOnlyList<AuthenticationFactorProviderKey> _allowedProviders;

        /// <summary>Gets the identity scope.</summary>
        public Guid IdentityScopeId { get; }

        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the policy mode.</summary>
        public MfaPolicyMode Mode { get; }

        /// <summary>Gets the providers explicitly allowed by this policy.</summary>
        public IReadOnlyList<AuthenticationFactorProviderKey> AllowedProviders => _allowedProviders;

        /// <summary>Initializes a generic MFA policy.</summary>
        public MfaPolicy(
            Guid identityScopeId,
            ApplicationKey application,
            MfaPolicyMode mode,
            IEnumerable<AuthenticationFactorProviderKey> allowedProviders)
        {
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));

            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(allowedProviders);

            IdentityScopeId = identityScopeId;
            Application = application;
            Mode = ModelGuard.DefinedEnum(mode, nameof(mode));

            var providers = allowedProviders
                .Select(provider => provider ?? throw new ArgumentException(
                    "Allowed providers cannot contain null values.",
                    nameof(allowedProviders)))
                .Distinct()
                .OrderBy(provider => provider.Value, StringComparer.Ordinal)
                .ToArray();

            if (mode == MfaPolicyMode.Disabled && providers.Length != 0)
            {
                throw new ArgumentException(
                    "A disabled MFA policy cannot allow providers.",
                    nameof(allowedProviders));
            }

            if (mode != MfaPolicyMode.Disabled && providers.Length == 0)
            {
                throw new ArgumentException(
                    "An enabled MFA policy must allow at least one provider.",
                    nameof(allowedProviders));
            }

            _allowedProviders = providers;
        }
    }
}
