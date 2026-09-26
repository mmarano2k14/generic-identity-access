using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Application.Security;
using IdentityAccess.Application.Authorization;

namespace IdentityAccess.Api
{

    internal static class AdministrationServiceRegistration
    {
        public static void AddIdentityAdministration(this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            if (!Has<IDatabaseRouteResolver>(services) || !Has<ISecurityAuditWriter>(services)) return;

            if (Has<IUserDirectoryStore>(services) &&
                Has<ITenantDirectoryStore>(services) &&
                Has<ITenantMembershipStore>(services) &&
                Has<IUserGroupStore>(services) &&
                Has<IGroupMembershipStore>(services) &&
                Has<IGroupMembershipMutationStore>(services))
            {
                services.AddSingleton<IDirectoryAdministrationService, DirectoryAdministrationService>();
            }

            if (Has<IIdentityScopeAssignedCapabilityReader>(services) &&
                Has<ITenantMembershipStore>(services))
            {
                services.AddSingleton<IEffectiveAdministrationContextService, EffectiveAdministrationContextService>();
            }

            if (Has<ITenantMembershipStore>(services))
            {
                services.AddSingleton<IAdministrationTenantVisibilityService, AdministrationTenantVisibilityService>();
            }

            if (Has<ITenantUserReadStore>(services))
            {
                services.AddSingleton<ITenantUserAdministrationService, TenantUserAdministrationService>();
            }

            if (Has<IApplicationSecurityCatalogStore>(services))
            {
                services.AddSingleton<ApplicationSecurityManifestFingerprint>();
                services.AddSingleton<IApplicationSecurityCatalogAdministrationService, ApplicationSecurityCatalogAdministrationService>();
            }

            if (Has<IManagedPolicyStore>(services) &&
                Has<IManagedPolicyVersionStore>(services) &&
                Has<IManagedPolicyStatementStore>(services))
            {
                services.AddSingleton<IManagedPolicyAdministrationService, ManagedPolicyAdministrationService>();
            }

            if (Has<IManagedPolicyStore>(services) &&
                Has<IManagedGroupPolicyBindingStore>(services) &&
                Has<IManagedGroupPolicyBindingMutationStore>(services))
            {
                services.AddSingleton<IManagedPolicyBindingAdministrationService, ManagedPolicyBindingAdministrationService>();
            }

            if (Has<IApplicationScopeTypeStore>(services) && Has<IResourceScopeStore>(services))
            {
                services.AddSingleton<IResourceScopeAdministrationService, ResourceScopeAdministrationService>();
            }

            if (Has<ISecurityAuditReader>(services))
            {
                services.AddSingleton<ISecurityAuditAdministrationService, SecurityAuditAdministrationService>();
            }

            if (Has<IIdentityScopeAdministrationGroupStore>(services) &&
                Has<IIdentityScopeAdministrationMembershipStore>(services) &&
                Has<IIdentityScopeAdministrationPolicyStore>(services) &&
                Has<IIdentityScopeAdministrationPolicyStatementStore>(services) &&
                Has<IIdentityScopeAdministrationBindingStore>(services))
            {
                services.AddSingleton<
                    IIdentityScopeAuthorityAdministrationService,
                    IdentityScopeAuthorityAdministrationService>();
            }
        }

        private static bool Has<T>(IServiceCollection services) =>
            services.Any(descriptor => descriptor.ServiceType == typeof(T));
    }
}
