using IdentityAccess.Application.Authentication.Mfa;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>Registers the provider-neutral authentication-factor registry.</summary>
    internal static class AuthenticationFactorProviderServiceCollectionExtensions
    {
        /// <summary>Registers the provider-neutral registry even when no concrete provider is installed.</summary>
        internal static IServiceCollection AddIdentityAccessAuthenticationFactorProviderRegistry(
            this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.TryAddSingleton<IAuthenticationFactorProviderRegistry, AuthenticationFactorProviderRegistry>();
            return services;
        }
    }
}
