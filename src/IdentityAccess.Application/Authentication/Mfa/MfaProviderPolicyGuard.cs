using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Evaluates concrete factor-provider use against provider-neutral MFA policy.</summary>
    public sealed class MfaProviderPolicyGuard(IMfaPolicyStore policies) : IMfaProviderPolicyGuard
    {
        /// <inheritdoc />
        public async Task<MfaProviderPolicyDecision> EvaluateAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            ApplicationKey application,
            AuthenticationFactorProviderKey provider,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(route);
            if (identityScopeId == Guid.Empty)
                throw new ArgumentException("Identity scope is required.", nameof(identityScopeId));
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(provider);

            if (route.Request.IdentityScopeId != identityScopeId || route.Request.Application != application)
                throw new InvalidOperationException("The resolved route does not match the MFA policy scope.");

            var record = await policies
                .GetAsync(route, identityScopeId, application, cancellationToken)
                .ConfigureAwait(false);

            if (record is null)
                return MfaProviderPolicyDecision.PolicyNotConfigured;

            if (record.Value.Mode == MfaPolicyMode.Disabled)
                return MfaProviderPolicyDecision.MfaDisabled;

            return record.Value.AllowedProviders.Contains(provider)
                ? MfaProviderPolicyDecision.Allowed
                : MfaProviderPolicyDecision.ProviderNotAllowed;
        }
    }
}
