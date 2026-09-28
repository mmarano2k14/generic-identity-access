using IdentityAccess.Authorization;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Protects the legacy/direct user-id membership creation surface from tenant-scoped callers.
    /// Tenant-scoped administration uses the exact-login candidate endpoint instead.
    /// </summary>
    internal sealed class TenantMembershipCreationAuthorizationGuard(
        IIdentityScopeAuthorizationService scopeAuthorization,
        AdministrationAuthorizationOptions options)
        : ITenantMembershipCreationAuthorizationGuard
    {
        public async ValueTask<AdministrationAccessResult> AuthorizeDirectCreateAsync(
            AdministrationRequestContext context,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            var result = await scopeAuthorization.AuthorizeAsync(
                new IdentityScopeAuthorizationRequest(
                    context.Subject.IdentityScopeId,
                    context.Subject,
                    context.Application,
                    options.RbacProject,
                    options.RbacNamespace,
                    new CapabilityKey(
                        IdentityAccessAdministrationCapabilities.Resource,
                        IdentityAccessAdministrationCapabilities.TenantMemberships,
                        IdentityAccessAdministrationCapabilities.Write)),
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
