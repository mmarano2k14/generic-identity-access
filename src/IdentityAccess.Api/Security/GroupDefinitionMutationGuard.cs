using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Normal groups follow tenant authorization. Once a real group is marked reusable, mutations
    /// to its reusable definition require identity-scope authority so tenant administration cannot
    /// silently change what another tenant may clone.
    /// </summary>
    internal sealed class GroupDefinitionMutationGuard(
        IAdministrationRequestContextResolver contextResolver,
        IIdentityScopeAuthorizationService scopeAuthorization,
        IDatabaseRouteResolver routeResolver,
        IUserGroupStore groups,
        AdministrationAuthorizationOptions options) : IGroupDefinitionMutationGuard
    {
        public async ValueTask<AdministrationAccessResult> AuthorizeAsync(
            HttpContext httpContext,
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            string feature,
            string action,
            CancellationToken cancellationToken)
        {
            var route = await routeResolver.ResolveAsync(
                new DatabaseRouteRequest(application, identityScopeId), cancellationToken).ConfigureAwait(false);
            var reference = new GroupReference(new TenantReference(identityScopeId, tenantId), application, groupId);
            var group = await groups.GetAsync(route, reference, cancellationToken).ConfigureAwait(false);
            if (group is null || !group.Value.IsTemplate) return AdministrationAccessResult.Allow();

            var authentication = await contextResolver.ResolveAsync(httpContext, cancellationToken).ConfigureAwait(false);
            if (authentication.Decision == AdministrationAuthenticationDecision.Unavailable)
                return AdministrationAccessResult.Unavailable(AdministrationAccessFailureCode.AuthenticationUnavailable);
            if (authentication.Decision != AdministrationAuthenticationDecision.Authenticated || authentication.Context is null)
                return AdministrationAccessResult.Unauthenticated(AdministrationAccessFailureCode.AuthenticationRequired);
            if (!AdministrationRequestBoundary.Matches(httpContext, authentication.Context) ||
                authentication.Context.Subject.IdentityScopeId != identityScopeId ||
                authentication.Context.Application != application)
                return AdministrationAccessResult.Deny(AdministrationAccessFailureCode.AuthenticationContextMismatch);

            var result = await scopeAuthorization.AuthorizeAsync(
                new IdentityScopeAuthorizationRequest(
                    identityScopeId,
                    authentication.Context.Subject,
                    application,
                    options.RbacProject,
                    options.RbacNamespace,
                    new CapabilityKey(IdentityAccessAdministrationCapabilities.Resource, feature, action)),
                cancellationToken).ConfigureAwait(false);

            return result.Decision switch
            {
                IdentityAuthorizationDecision.Allowed => AdministrationAccessResult.Allow(),
                IdentityAuthorizationDecision.Denied => AdministrationAccessResult.Deny(),
                _ => AdministrationAccessResult.Unavailable(AdministrationAccessFailureCode.AuthorizationTechnicalFailure)
            };
        }
    }
}
