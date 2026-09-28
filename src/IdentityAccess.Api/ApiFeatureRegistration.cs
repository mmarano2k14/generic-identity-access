using IdentityAccess.Api.Features;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Mfa.Recovery;
using IdentityAccess.Mfa.Totp;
using IdentityAccess.Mfa.WebAuthn;

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
            Register<ITenantUserAdministrationService>(builder.Services);
            Register<ITenantMembershipCandidateService>(builder.Services);
            Register<ITenantGroupAssignmentDelegationGuard>(builder.Services);
            Register<IGroupDefinitionMutationGuard>(builder.Services);
            Register<ITenantMembershipCreationAuthorizationGuard>(builder.Services);
            Register<IEffectiveAdministrationContextService>(builder.Services);
            Register<IApplicationSecurityCatalogAdministrationService>(builder.Services);
            Register<IManagedPolicyAdministrationService>(builder.Services);
            Register<IManagedPolicyBindingAdministrationService>(builder.Services);
            Register<IResourceScopeAdministrationService>(builder.Services);
            Register<ISecurityAuditAdministrationService>(builder.Services);
            Register<IIdentityScopeAuthorityAdministrationService>(builder.Services);
            Register<ILocalAuthenticationService>(builder.Services);
            Register<ICredentialAdministrationService>(builder.Services);
            Register<ISelfServiceCredentialService>(builder.Services);
            Register<ISessionAdministrationService>(builder.Services);
            Register<IMfaAdministrationService>(builder.Services);
            Register<IAuthenticationAssuranceService>(builder.Services);
            Register<ITotpAuthenticationFactorService>(builder.Services);
            Register<IRecoveryAuthenticationFactorService>(builder.Services);
            Register<IRecoveryPasswordResetService>(builder.Services);
            Register<IWebAuthnAuthenticationService>(builder.Services);
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
