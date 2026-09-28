using IdentityAccess.Domain;

namespace IdentityAccess.Api.Security
{
    /// <summary>Requires identity-scope authority before a reusable group definition can be changed.</summary>
    public interface IGroupDefinitionMutationGuard
    {
        ValueTask<AdministrationAccessResult> AuthorizeAsync(
            HttpContext httpContext,
            Guid identityScopeId,
            Guid tenantId,
            ApplicationKey application,
            Guid groupId,
            string feature,
            string action,
            CancellationToken cancellationToken);
    }
}
