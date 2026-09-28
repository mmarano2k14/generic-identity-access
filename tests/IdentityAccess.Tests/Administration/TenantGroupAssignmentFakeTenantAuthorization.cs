using IdentityAccess.Application.Authorization;
using IdentityAccess.Authorization;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult result) : IIdentityAuthorizationService
    {
        public int CallCount { get; private set; }
        public IdentityAuthorizationRequest? Request { get; private set; }

        public ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            Request = request;
            return ValueTask.FromResult(result);
        }
    }
}
