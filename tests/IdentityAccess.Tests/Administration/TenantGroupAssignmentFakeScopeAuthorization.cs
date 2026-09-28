using IdentityAccess.Application.Authorization;
using IdentityAccess.Authorization;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult result) : IIdentityScopeAuthorizationService
    {
        public ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityScopeAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }
}
