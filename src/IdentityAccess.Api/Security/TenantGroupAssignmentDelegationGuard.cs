using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Allows identity-scope administrators to delegate directly. Tenant-scoped administrators may
    /// assign or clone a group's grants only when every concrete, non-descendant grant is also
    /// allowed for the actor at the target tenant/resource. Wildcard and descendant-expanding grants
    /// remain non-delegable from tenant scope until an explicit broader delegation contract exists.
    /// </summary>
    internal sealed class TenantGroupAssignmentDelegationGuard(
        IDatabaseRouteResolver routeResolver,
        IGroupCapabilityGrantReader groupGrants,
        IIdentityAuthorizationService tenantAuthorization,
        IIdentityScopeAuthorizationService scopeAuthorization,
        AdministrationAuthorizationOptions options)
        : ITenantGroupAssignmentDelegationGuard
    {
        public ValueTask<AdministrationAccessResult> AuthorizeAssignmentAsync(
            AdministrationRequestContext context,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            CancellationToken cancellationToken) =>
            AuthorizeAsync(context, tenantId, tenantId, application, groupId, cancellationToken);

        public ValueTask<AdministrationAccessResult> AuthorizeGrantCopyAsync(
            AdministrationRequestContext context,
            Guid sourceTenantId,
            Guid targetTenantId,
            ApplicationKey application,
            Guid groupId,
            CancellationToken cancellationToken) =>
            AuthorizeAsync(context, sourceTenantId, targetTenantId, application, groupId, cancellationToken);

        private async ValueTask<AdministrationAccessResult> AuthorizeAsync(
            AdministrationRequestContext context,
            Guid sourceTenantId,
            Guid targetTenantId,
            ApplicationKey application,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(application);
            if (sourceTenantId == Guid.Empty || targetTenantId == Guid.Empty || groupId == Guid.Empty)
                return AdministrationAccessResult.Deny();
            if (context.Application != application)
                return AdministrationAccessResult.Deny(AdministrationAccessFailureCode.AuthenticationContextMismatch);

            var administrationCapability = new CapabilityKey(
                IdentityAccessAdministrationCapabilities.Resource,
                IdentityAccessAdministrationCapabilities.GroupMemberships,
                IdentityAccessAdministrationCapabilities.Write);

            var scopeResult = await scopeAuthorization.AuthorizeAsync(
                new IdentityScopeAuthorizationRequest(
                    context.Subject.IdentityScopeId,
                    context.Subject,
                    application,
                    options.RbacProject,
                    options.RbacNamespace,
                    administrationCapability),
                cancellationToken).ConfigureAwait(false);

            if (scopeResult.Decision == IdentityAuthorizationDecision.Allowed)
                return AdministrationAccessResult.Allow();

            var sourceTenant = new TenantReference(context.Subject.IdentityScopeId, sourceTenantId);
            var targetTenant = new TenantReference(context.Subject.IdentityScopeId, targetTenantId);
            var route = await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, context.Subject.IdentityScopeId),
                cancellationToken).ConfigureAwait(false);
            var grants = await groupGrants.ListAsync(
                route,
                new GroupReference(sourceTenant, application, groupId),
                cancellationToken).ConfigureAwait(false);

            foreach (var grant in grants)
            {
                if (!grant.Pattern.IsConcrete || grant.IncludeDescendants)
                    return AdministrationAccessResult.Deny();

                var targetScope = grant.TargetScope is null
                    ? null
                    : new ResourceScopeReference(targetTenant, application, grant.TargetScope.ResourceScopeId);
                var result = await tenantAuthorization.AuthorizeAsync(
                    new IdentityAuthorizationRequest(
                        targetTenant,
                        context.Subject,
                        application,
                        options.RbacProject,
                        options.RbacNamespace,
                        grant.Pattern.ToConcreteCapability(),
                        targetScope),
                    cancellationToken).ConfigureAwait(false);

                if (result.Decision == IdentityAuthorizationDecision.Denied)
                    return AdministrationAccessResult.Deny();
                if (result.Decision != IdentityAuthorizationDecision.Allowed)
                    return AdministrationAccessResult.Unavailable(
                        AdministrationAccessFailureCode.AuthorizationTechnicalFailure);
            }

            return AdministrationAccessResult.Allow();
        }
    }
}
