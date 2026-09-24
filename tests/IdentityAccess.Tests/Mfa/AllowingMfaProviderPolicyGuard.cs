using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa
{
    internal sealed class AllowingMfaProviderPolicyGuard : IMfaProviderPolicyGuard
    {
        public Task<MfaProviderPolicyDecision> EvaluateAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            AuthenticationFactorProviderKey provider,
            CancellationToken cancellationToken) =>
            Task.FromResult(MfaProviderPolicyDecision.Allowed);
    }
}
