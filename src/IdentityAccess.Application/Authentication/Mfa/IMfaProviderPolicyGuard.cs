using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Evaluates provider use against the generic MFA policy on an already resolved database route.</summary>
    public interface IMfaProviderPolicyGuard
    {
        /// <summary>Evaluates whether the provider is allowed for the supplied scope and application.</summary>
        Task<MfaProviderPolicyDecision> EvaluateAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            AuthenticationFactorProviderKey provider,
            CancellationToken cancellationToken);
    }
}
