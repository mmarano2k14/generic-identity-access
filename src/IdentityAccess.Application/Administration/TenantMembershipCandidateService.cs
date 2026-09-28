using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Provides exact-login candidate resolution for tenant membership without directory search.</summary>
    public sealed class TenantMembershipCandidateService(
        IDatabaseRouteResolver routeResolver,
        IPasswordCredentialStore credentials,
        IUserDirectoryStore users,
        ITenantMembershipStore memberships) : ITenantMembershipCandidateService
    {
        public async Task<TenantMembershipCandidate?> FindByLoginAsync(Guid identityScopeId, ApplicationKey application,
            Guid tenantId, string loginIdentifier, CancellationToken cancellationToken)
        {
            var login = new LoginIdentifier(loginIdentifier);
            var route = await routeResolver.ResolveAsync(new DatabaseRouteRequest(application, identityScopeId), cancellationToken);
            var credential = await credentials.FindByLoginAsync(route, login.NormalizedValue, cancellationToken);
            if (credential is null) return null;

            var user = await users.GetAsync(route, credential.Value.Subject, cancellationToken);
            if (user is null) return null;

            var tenant = new TenantReference(identityScopeId, tenantId);
            var existing = await memberships.FindAsync(route, tenant, credential.Value.Subject, cancellationToken);
            return new TenantMembershipCandidate(
                credential.Value.Subject.UserId,
                user.Value.DisplayName,
                user.Value.Status,
                existing?.Value.MembershipId,
                existing?.Value.Status);
        }
    }
}
