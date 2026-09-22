namespace IdentityAccess.Authorization
{
    /// <summary>
    /// Defines authorization orchestration for identity-scope administration operations.
    /// </summary>
    public interface IIdentityScopeAuthorizationService
    {
        /// <summary>Evaluates the requested identity-scope capability.</summary>
        ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityScopeAuthorizationRequest request,
            CancellationToken cancellationToken);
    }
}
