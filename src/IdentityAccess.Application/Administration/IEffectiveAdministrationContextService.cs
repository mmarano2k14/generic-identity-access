using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Resolves tenant visibility from trusted identity-scope authority and active memberships.</summary>
    public interface IEffectiveAdministrationContextService
    {
        /// <summary>Resolves the effective administration context for one authenticated subject.</summary>
        Task<EffectiveAdministrationContext> ResolveAsync(
            Guid identityScopeId,
            ApplicationKey application,
            SubjectReference subject,
            CancellationToken cancellationToken);
    }
}
