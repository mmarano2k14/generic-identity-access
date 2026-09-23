using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Domain;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>Immutable registry over provider instances registered in dependency injection.</summary>
    internal sealed class AuthenticationFactorProviderRegistry : IAuthenticationFactorProviderRegistry
    {
        private readonly IReadOnlyDictionary<string, IAuthenticationFactorProvider> _providers;
        private readonly IReadOnlyList<AuthenticationFactorProviderDescriptor> _descriptors;

        /// <summary>Initializes the registry and rejects duplicate provider keys.</summary>
        public AuthenticationFactorProviderRegistry(IEnumerable<IAuthenticationFactorProvider> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);

            var dictionary = new Dictionary<string, IAuthenticationFactorProvider>(StringComparer.Ordinal);
            foreach (var provider in providers)
            {
                ArgumentNullException.ThrowIfNull(provider);
                var key = provider.Descriptor.Key.Value;
                if (!dictionary.TryAdd(key, provider))
                    throw new InvalidOperationException($"Authentication-factor provider '{key}' is registered more than once.");
            }

            _providers = dictionary;
            _descriptors = dictionary.Values
                .Select(provider => provider.Descriptor)
                .OrderBy(descriptor => descriptor.Key.Value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <inheritdoc />
        public IReadOnlyList<AuthenticationFactorProviderDescriptor> List() => _descriptors;

        /// <inheritdoc />
        public IAuthenticationFactorProvider? Find(AuthenticationFactorProviderKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _providers.GetValueOrDefault(key.Value);
        }
    }
}
