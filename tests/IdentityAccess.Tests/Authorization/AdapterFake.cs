using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    internal sealed class AdapterFake(Func<RbacAuthorizationRequest, RbacAuthorizationResult> evaluate)
        : IRbacAuthorizationAdapter
    {
        public int CallCount { get; private set; }

        public ValueTask<RbacAuthorizationResult> AuthorizeAsync(
            RbacAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return ValueTask.FromResult(evaluate(request));
        }
    }
}
