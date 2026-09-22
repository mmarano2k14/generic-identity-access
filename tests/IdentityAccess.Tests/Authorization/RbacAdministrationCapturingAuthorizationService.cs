using IdentityAccess.Authorization;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>Captures tenant/resource authorization requests emitted by the administration bridge.</summary>
    internal sealed class RbacAdministrationCapturingAuthorizationService(
        IdentityAuthorizationResult result)
        : IIdentityAuthorizationService
    {
        /// <summary>Gets the most recent tenant/resource authorization request.</summary>
        public IdentityAuthorizationRequest? Request { get; private set; }

        /// <inheritdoc />
        public ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return ValueTask.FromResult(result);
        }
    }
}
