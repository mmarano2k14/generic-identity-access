using IdentityAccess.Api.Features;
using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;

namespace IdentityAccess.Api
{
    /// <summary>
    /// Registers optional API feature handles after all configurable application services have
    /// been registered.
    /// </summary>
    internal static class ApiFeatureRegistration
    {
        /// <summary>
        /// Registers controller-facing optional feature handles for configurable services.
        /// </summary>
        public static void AddIdentityApiFeatures(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            Register<IDatabaseRouteResolver>(builder.Services);
            Register<IIdentityDatabaseConnectionFactory>(builder.Services);
            Register<IDirectoryAdministrationService>(builder.Services);
            Register<IPolicyAdministrationService>(builder.Services);
            Register<IResourceScopeAdministrationService>(builder.Services);
            Register<IIdentityScopeAuthorityAdministrationService>(builder.Services);
            Register<ILocalAuthenticationService>(builder.Services);
            Register<ICredentialAdministrationService>(builder.Services);
            Register<ISessionAdministrationService>(builder.Services);
            Register<IMfaAdministrationService>(builder.Services);
            Register<IOidcAuthorizationService>(builder.Services);
        }

        private static void Register<TService>(IServiceCollection services)
            where TService : class
        {
            services.AddSingleton(provider =>
                new OptionalFeature<TService>(provider.GetService<TService>()));
        }
    }
}
