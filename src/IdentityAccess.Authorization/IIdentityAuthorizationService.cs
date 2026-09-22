

namespace IdentityAccess.Authorization
{

    /// <summary>Defines authorization orchestration for a subject, tenant, application, and resource context.</summary>
    public interface IIdentityAuthorizationService
    {
        /// <summary>Evaluates the requested capability for the supplied subject and resource context.</summary>
        ValueTask<IdentityAuthorizationResult> AuthorizeAsync(
            IdentityAuthorizationRequest request,
            CancellationToken cancellationToken);
    }
}
