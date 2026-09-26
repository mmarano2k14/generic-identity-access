using IdentityAccess.Application.Administration;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Authorizes administration requests against the trusted administration authority hierarchy.
    /// Identity-scope grants may authorize tenant/resource routes inside the same identity scope;
    /// tenant grants are then evaluated as the narrower fallback. Identity-scope-only routes never
    /// borrow tenant authority. Final wildcard-aware decisions remain external.
    /// </summary>
    internal sealed class RbacAdministrationRequestAuthorizer(
        IAdministrationRequestContextResolver contextResolver,
        IIdentityAuthorizationService tenantAuthorizationService,
        IIdentityScopeAuthorizationService scopeAuthorizationService,
        IAdministrationTenantVisibilityService tenantVisibilityService,
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

            try
            {
                if (AdministrationAuthorizationTargetResolver.TryResolve(
                        httpContext,
                        context,
                        out var tenantTarget) &&
                    tenantTarget is not null)
                {
                    return await AuthorizeTenantTargetAsync(
                            context,
                            tenantTarget,
                            capability,
                            resource,
                            feature,
                            action,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                if (!httpContext.Request.RouteValues.ContainsKey(
                        "tenantId"))
                {
                    var scopeResult = await AuthorizeIdentityScopeAsync(
                            context,
                            capability,
                            cancellationToken)
                        .ConfigureAwait(false);

                    return MapResult(
                        scopeResult,
                        resource,
                        feature,
                        action);
                }

                return AdministrationAccessResult.Unavailable(
                    AdministrationAccessFailureCode.AuthorizationTargetUnavailable);
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
        }


        private async ValueTask<AdministrationAccessResult> AuthorizeTenantTargetAsync(
            AdministrationRequestContext context,
            AdministrationAuthorizationTarget tenantTarget,
            CapabilityKey capability,
            string resource,
            string feature,
            string action,
            CancellationToken cancellationToken)
        {
            var scopeResult = await AuthorizeIdentityScopeAsync(
                    context,
                    capability,
                    cancellationToken)
                .ConfigureAwait(false);

            if (scopeResult.Decision == IdentityAuthorizationDecision.Allowed)
            {
                return AdministrationAccessResult.Allow();
            }

            var hasActiveTenantMembership = await tenantVisibilityService
                .HasActiveMembershipAsync(
                    context.Subject.IdentityScopeId,
                    context.Application,
                    context.Subject,
                    tenantTarget.Tenant.TenantId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!hasActiveTenantMembership)
            {
                if (scopeResult.Decision == IdentityAuthorizationDecision.Denied)
                {
                    return AdministrationAccessResult.Deny(
                        AdministrationAccessFailureCode.TenantContextOutsideVisibility);
                }

                return AdministrationAccessResult.Unavailable(
                    AdministrationAccessFailureCode.AuthorizationTechnicalFailure);
            }

            var tenantResult = await tenantAuthorizationService
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

            if (tenantResult.Decision == IdentityAuthorizationDecision.Allowed)
            {
                return AdministrationAccessResult.Allow();
            }

            if (scopeResult.Decision == IdentityAuthorizationDecision.Denied &&
                tenantResult.Decision == IdentityAuthorizationDecision.Denied)
            {
                return AdministrationAccessResult.Deny();
            }

            LogComposedTechnicalFailure(
                scopeResult,
                tenantResult,
                resource,
                feature,
                action);

            return AdministrationAccessResult.Unavailable(
                AdministrationAccessFailureCode.AuthorizationTechnicalFailure);
        }

        private ValueTask<IdentityAuthorizationResult> AuthorizeIdentityScopeAsync(
            AdministrationRequestContext context,
            CapabilityKey capability,
            CancellationToken cancellationToken) =>
            scopeAuthorizationService.AuthorizeAsync(
                new IdentityScopeAuthorizationRequest(
                    context.Subject.IdentityScopeId,
                    context.Subject,
                    context.Application,
                    options.RbacProject,
                    options.RbacNamespace,
                    capability),
                cancellationToken);

        private void LogComposedTechnicalFailure(
            IdentityAuthorizationResult scopeResult,
            IdentityAuthorizationResult tenantResult,
            string resource,
            string feature,
            string action)
        {
            logger.LogWarning(
                "Administration authorization could not complete after identity-scope and tenant evaluation. Scope decision {ScopeDecision} ({ScopeFailureCode}/{ScopeRbacFailureCode}); tenant decision {TenantDecision} ({TenantFailureCode}/{TenantRbacFailureCode}); capability {Resource}/{Feature}/{Action}.",
                scopeResult.Decision,
                scopeResult.FailureCode,
                scopeResult.RbacFailureCode,
                tenantResult.Decision,
                tenantResult.FailureCode,
                tenantResult.RbacFailureCode,
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
