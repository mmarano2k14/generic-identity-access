using IdentityAccess.Authorization;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>Captures identity-scope authorization requests emitted by the administration bridge.</summary>
    internal sealed class RbacAdministrationCapturingIdentityScopeAuthorizationService(
        IdentityAuthorizationResult result)
        : IIdentityScopeAuthorizationService
    {
        /// <summary>Gets the most recent identity-scope authorization request.</summary>
        public IdentityScopeAuthorizationRequest? Request { get; private set; }

        /// <inheritdoc />
        public ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityScopeAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return ValueTask.FromResult(result);
        }
    }
}
