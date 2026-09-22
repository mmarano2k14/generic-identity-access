using IdentityAccess.Authorization;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Authorizes administration requests against the correct authority boundary: tenant/resource
    /// routes use tenant grants, while identity-scope-only routes use dedicated scope
    /// administration grants. Final wildcard-aware decisions remain external.
    /// </summary>
    internal sealed class RbacAdministrationRequestAuthorizer(
        IAdministrationRequestContextResolver contextResolver,
        IIdentityAuthorizationService tenantAuthorizationService,
        IIdentityScopeAuthorizationService scopeAuthorizationService,
        AdministrationAuthorizationOptions options,
        ILogger<RbacAdministrationRequestAuthorizer> logger)
        : IAdministrationRequestAuthorizer
    {
        /// <inheritdoc />
        public bool CapabilityAuthorizationAvailable => true;

        /// <inheritdoc />
        public async ValueTask<AdministrationAccessResult> AuthorizeAsync(
            HttpContext httpContext,
            string resource,
            string feature,
            string action,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentException.ThrowIfNullOrWhiteSpace(resource);
            ArgumentException.ThrowIfNullOrWhiteSpace(feature);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);

            var authentication = await contextResolver
                .ResolveAsync(
                    httpContext,
                    cancellationToken)
                .ConfigureAwait(false);

            if (authentication.Decision ==
                AdministrationAuthenticationDecision.Unavailable)
            {
                return AdministrationAccessResult.Unavailable(
                    AdministrationAccessFailureCode.AuthenticationUnavailable);
            }

            if (authentication.Decision !=
                    AdministrationAuthenticationDecision.Authenticated ||
                authentication.Context is null)
            {
                return AdministrationAccessResult.Unauthenticated(
                    AdministrationAccessFailureCode.AuthenticationRequired);
            }

            var context = authentication.Context;

            if (!AdministrationRequestBoundary.Matches(
                    httpContext,
                    context))
            {
                return AdministrationAccessResult.Deny(
                    AdministrationAccessFailureCode.AuthenticationContextMismatch);
            }

            AdministrationAuditActivityContext.Apply(
                context);

            httpContext.Features.Set(
                new AdministrationRequestContextFeature(
                    context));

            var capability = new CapabilityKey(
                resource,
                feature,
                action);

            IdentityAuthorizationResult result;

            try
            {
                if (AdministrationAuthorizationTargetResolver.TryResolve(
                        httpContext,
                        context,
                        out var tenantTarget) &&
                    tenantTarget is not null)
                {
                    result = await tenantAuthorizationService
                        .AuthorizeAsync(
                            new IdentityAuthorizationRequest(
                                tenantTarget.Tenant,
                                context.Subject,
                                context.Application,
                                options.RbacProject,
                                options.RbacNamespace,
                                capability,
                                tenantTarget.ResourceScope),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (!httpContext.Request.RouteValues.ContainsKey(
                             "tenantId"))
                {
                    result = await scopeAuthorizationService
                        .AuthorizeAsync(
                            new IdentityScopeAuthorizationRequest(
                                context.Subject.IdentityScopeId,
                                context.Subject,
                                context.Application,
                                options.RbacProject,
                                options.RbacNamespace,
                                capability),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    return AdministrationAccessResult.Unavailable(
                        AdministrationAccessFailureCode.AuthorizationTargetUnavailable);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Administration authorization orchestration threw for capability {Resource}/{Feature}/{Action}.",
                    resource,
                    feature,
                    action);

                return AdministrationAccessResult.Unavailable(
                    AdministrationAccessFailureCode.AuthorizationTechnicalFailure);
            }

            return MapResult(
                result,
                resource,
                feature,
                action);
        }

        private AdministrationAccessResult MapResult(
            IdentityAuthorizationResult result,
            string resource,
            string feature,
            string action)
        {
            switch (result.Decision)
            {
                case IdentityAuthorizationDecision.Allowed:
                    return AdministrationAccessResult.Allow();

                case IdentityAuthorizationDecision.Denied:
                    return AdministrationAccessResult.Deny();

                case IdentityAuthorizationDecision.TechnicalFailure:
                    logger.LogWarning(
                        "Administration authorization technical failure {AuthorizationFailureCode} with external RBAC failure {RbacFailureCode} for capability {Resource}/{Feature}/{Action}.",
                        result.FailureCode,
                        result.RbacFailureCode,
                        resource,
                        feature,
                        action);

                    return AdministrationAccessResult.Unavailable(
                        AdministrationAccessFailureCode.AuthorizationTechnicalFailure);

                default:
                    logger.LogWarning(
                        "Administration authorization returned an unknown decision for capability {Resource}/{Feature}/{Action}.",
                        resource,
                        feature,
                        action);

                    return AdministrationAccessResult.Unavailable(
                        AdministrationAccessFailureCode.AuthorizationTechnicalFailure);
            }
        }
    }
}
