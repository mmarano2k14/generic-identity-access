using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Registers trusted administration authentication and the strongest configured fail-closed
    /// authorization implementation.
    /// </summary>
    internal static class AdministrationSecurityServiceRegistration
    {
        /// <summary>Registers the administration security pipeline.</summary>
        public static void AddIdentityAdministrationSecurity(
            this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.TryAddSingleton<LocalSessionAdministrationRequestContextResolver>();

            var bearerValidationConfigured =
                builder.Services.Any(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(IOidcAccessTokenValidator));

            if (bearerValidationConfigured)
            {
                builder.Services.TryAddSingleton<BearerAdministrationRequestContextResolver>();
                builder.Services.Replace(
                    ServiceDescriptor.Singleton<
                        IAdministrationRequestContextResolver,
                        CompositeAdministrationRequestContextResolver>());
            }
            else
            {
                builder.Services.Replace(
                    ServiceDescriptor.Singleton<IAdministrationRequestContextResolver>(
                        provider =>
                            provider.GetRequiredService<
                                LocalSessionAdministrationRequestContextResolver>()));
            }

            var tenantAuthorizationConfigured =
                builder.Services.Any(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(IIdentityAuthorizationService));

            var scopeAuthorizationConfigured =
                builder.Services.Any(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(IIdentityScopeAuthorizationService));

            var tenantVisibilityConfigured =
                builder.Services.Any(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(IAdministrationTenantVisibilityService));

            var authorizationConfigured =
                tenantAuthorizationConfigured &&
                scopeAuthorizationConfigured &&
                tenantVisibilityConfigured;

            builder.Services.Replace(
                authorizationConfigured
                    ? ServiceDescriptor.Singleton<
                        IAdministrationRequestAuthorizer,
                        RbacAdministrationRequestAuthorizer>()
                    : ServiceDescriptor.Singleton<
                        IAdministrationRequestAuthorizer,
                        SessionBackedAdministrationRequestAuthorizer>());
        }
    }
}
