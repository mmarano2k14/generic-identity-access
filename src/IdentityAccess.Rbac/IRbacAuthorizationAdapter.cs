

namespace IdentityAccess.Rbac
{

    /// <summary>Defines the anti-corruption boundary to an external RBAC decision engine.</summary>
    public interface IRbacAuthorizationAdapter
    {
        /// <summary>Evaluates an authorization request using the configured external RBAC implementation.</summary>
        ValueTask<RbacAuthorizationResult> AuthorizeAsync(
            RbacAuthorizationRequest request,
            CancellationToken cancellationToken);
    }
}
