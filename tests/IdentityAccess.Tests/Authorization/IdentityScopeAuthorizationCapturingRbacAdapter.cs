using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>Captures the RBAC request emitted by identity-scope authorization tests.</summary>
    internal sealed class IdentityScopeAuthorizationCapturingRbacAdapter(
        RbacAuthorizationResult result)
        : IRbacAuthorizationAdapter
    {
        /// <summary>Gets the most recent request received by the adapter.</summary>
        public RbacAuthorizationRequest? Request { get; private set; }

        /// <inheritdoc />
        public ValueTask<RbacAuthorizationResult> AuthorizeAsync(
            RbacAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return ValueTask.FromResult(result);
        }
    }
}
