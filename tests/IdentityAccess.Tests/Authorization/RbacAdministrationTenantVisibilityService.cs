using IdentityAccess.Application.Administration;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authorization
{
    internal sealed class RbacAdministrationTenantVisibilityService(bool hasActiveMembership)
        : IAdministrationTenantVisibilityService
    {
        public int Calls { get; private set; }

        public Task<bool> HasActiveMembershipAsync(
            Guid identityScopeId,
            ApplicationKey application,
            SubjectReference subject,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(hasActiveMembership);
        }
    }
}
