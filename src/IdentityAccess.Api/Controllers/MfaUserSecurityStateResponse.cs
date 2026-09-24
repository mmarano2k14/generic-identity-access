using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Provider-neutral effective MFA state for one user.</summary>
    public sealed record MfaUserSecurityStateResponse(
        bool PolicyConfigured,
        MfaPolicyMode? PolicyMode,
        bool MfaRequired,
        bool HasActiveVerificationFactor,
        bool HasActivePrimaryFactor,
        bool HasActiveRecoveryFactor,
        bool SatisfiesCurrentPolicy,
        IReadOnlyList<string> ActiveVerificationProviders,
        IReadOnlyList<string> ActivePrimaryProviders,
        IReadOnlyList<string> ActiveRecoveryProviders)
    {
        /// <summary>Creates the HTTP representation.</summary>
        public static MfaUserSecurityStateResponse From(MfaUserSecurityState state) =>
            new(
                state.PolicyConfigured,
                state.PolicyMode,
                state.MfaRequired,
                state.HasActiveVerificationFactor,
                state.HasActivePrimaryFactor,
                state.HasActiveRecoveryFactor,
                state.SatisfiesCurrentPolicy,
                Keys(state.ActiveVerificationProviders),
                Keys(state.ActivePrimaryProviders),
                Keys(state.ActiveRecoveryProviders));

        private static IReadOnlyList<string> Keys(
            IEnumerable<AuthenticationFactorProviderKey> providers) =>
            providers.Select(provider => provider.Value).ToArray();
    }
}
