using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Resolves tenant-scoped authorization targets from trusted route values. The current policy
    /// assignment model is tenant based, so identity-scope-only routes intentionally have no
    /// authorization target.
    /// </summary>
    internal static class AdministrationAuthorizationTargetResolver
    {
        /// <summary>
        /// Attempts to resolve the tenant and optional resource-scope authorization target.
        /// </summary>
        public static bool TryResolve(
            HttpContext httpContext,
            AdministrationRequestContext context,
            out AdministrationAuthorizationTarget? target)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(context);

            target = null;

            if (!TryGetGuidRouteValue(
                    httpContext,
                    "tenantId",
                    out var tenantId))
            {
                return false;
            }

            var tenant = new TenantReference(
                context.Subject.IdentityScopeId,
                tenantId);

            ResourceScopeReference? resourceScope = null;

            if (httpContext.Request.RouteValues.ContainsKey(
                    "resourceScopeId"))
            {
                if (!TryGetGuidRouteValue(
                        httpContext,
                        "resourceScopeId",
                        out var resourceScopeId))
                {
                    return false;
                }

                resourceScope = new ResourceScopeReference(
                    tenant,
                    context.Application,
                    resourceScopeId);
            }

            target = new AdministrationAuthorizationTarget(
                tenant,
                resourceScope);

            return true;
        }

        private static bool TryGetGuidRouteValue(
            HttpContext httpContext,
            string key,
            out Guid value)
        {
            if (!httpContext.Request.RouteValues.TryGetValue(
                    key,
                    out var raw) ||
                raw is null)
            {
                value = Guid.Empty;
                return false;
            }

            return Guid.TryParse(
                    Convert.ToString(
                        raw,
                        System.Globalization.CultureInfo.InvariantCulture),
                    out value) &&
                value != Guid.Empty;
        }
    }
}
