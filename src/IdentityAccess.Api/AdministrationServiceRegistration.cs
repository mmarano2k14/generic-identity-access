using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Application.Security;

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

            if (Has<IPermissionPolicyStore>(services) &&
                Has<IPolicyStatementStore>(services) &&
                Has<IGroupPolicyBindingStore>(services) &&
                Has<IGroupPolicyBindingMutationStore>(services))
            {
                services.AddSingleton<IPolicyAdministrationService, PolicyAdministrationService>();
            }

            if (Has<IApplicationScopeTypeStore>(services) && Has<IResourceScopeStore>(services))
            {
                services.AddSingleton<IResourceScopeAdministrationService, ResourceScopeAdministrationService>();
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
