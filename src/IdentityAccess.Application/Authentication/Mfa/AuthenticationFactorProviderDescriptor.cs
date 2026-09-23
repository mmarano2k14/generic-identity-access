using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Public metadata describing one registered authentication-factor provider.</summary>
    public sealed record AuthenticationFactorProviderDescriptor
    {
        /// <summary>Gets the stable provider key.</summary>
        public AuthenticationFactorProviderKey Key { get; }

        /// <summary>Gets the human-readable provider name.</summary>
        public string DisplayName { get; }

        /// <summary>Gets the generic provider capabilities.</summary>
        public AuthenticationFactorProviderCapabilities Capabilities { get; }

        /// <summary>Initializes provider metadata.</summary>
        public AuthenticationFactorProviderDescriptor(
            AuthenticationFactorProviderKey key,
            string displayName,
            AuthenticationFactorProviderCapabilities capabilities)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            if (capabilities == AuthenticationFactorProviderCapabilities.None ||
                (capabilities & ~(AuthenticationFactorProviderCapabilities.Enrollment |
                                  AuthenticationFactorProviderCapabilities.Verification |
                                  AuthenticationFactorProviderCapabilities.Recovery)) != AuthenticationFactorProviderCapabilities.None)
            {
                throw new ArgumentOutOfRangeException(nameof(capabilities));
            }

            Key = key;
            DisplayName = displayName.Trim();
            Capabilities = capabilities;
        }
    }
}
