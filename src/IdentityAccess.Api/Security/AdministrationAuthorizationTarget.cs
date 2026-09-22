using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Represents the tenant and optional resource-scope target selected by the current
    /// administration route.
    /// </summary>
    internal sealed record AdministrationAuthorizationTarget(
        TenantReference Tenant,
        ResourceScopeReference? ResourceScope);
}
