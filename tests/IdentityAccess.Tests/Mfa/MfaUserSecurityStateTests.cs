using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa
{
    public sealed class MfaUserSecurityStateTests
    {
        [Fact]
        public void Required_policy_without_active_factor_is_not_satisfied()
        {
            var state = new MfaUserSecurityState(
                policyConfigured: true,
                policyMode: MfaPolicyMode.Required,
                activeVerificationProviders: [],
                activePrimaryProviders: [],
                activeRecoveryProviders: []);

            Assert.True(state.MfaRequired);
            Assert.False(state.SatisfiesCurrentPolicy);
        }

        [Fact]
        public void Required_policy_with_active_verification_factor_is_satisfied()
        {
            var totp = new AuthenticationFactorProviderKey("totp");
            var state = new MfaUserSecurityState(
                policyConfigured: true,
                policyMode: MfaPolicyMode.Required,
                activeVerificationProviders: [totp, totp],
                activePrimaryProviders: [totp],
                activeRecoveryProviders: []);

            Assert.True(state.SatisfiesCurrentPolicy);
            Assert.Equal(new[] { totp }, state.ActiveVerificationProviders);
        }


        [Fact]
        public void Required_policy_is_not_satisfied_by_recovery_only()
        {
            var recovery = new AuthenticationFactorProviderKey("recovery");
            var state = new MfaUserSecurityState(
                policyConfigured: true,
                policyMode: MfaPolicyMode.Required,
                activeVerificationProviders: [recovery],
                activePrimaryProviders: [],
                activeRecoveryProviders: [recovery]);

            Assert.True(state.HasActiveVerificationFactor);
            Assert.True(state.HasActiveRecoveryFactor);
            Assert.False(state.HasActivePrimaryFactor);
            Assert.False(state.SatisfiesCurrentPolicy);
        }

        [Fact]
        public void Optional_policy_does_not_require_an_enrolled_factor()
        {
            var state = new MfaUserSecurityState(
                policyConfigured: true,
                policyMode: MfaPolicyMode.Optional,
                activeVerificationProviders: [],
                activePrimaryProviders: [],
                activeRecoveryProviders: []);

            Assert.False(state.MfaRequired);
            Assert.True(state.SatisfiesCurrentPolicy);
        }

        [Fact]
        public void Policy_configuration_and_mode_must_agree()
        {
            Assert.Throws<ArgumentException>(() => new MfaUserSecurityState(
                policyConfigured: false,
                policyMode: MfaPolicyMode.Required,
                activeVerificationProviders: [],
                activePrimaryProviders: [],
                activeRecoveryProviders: []));
        }
    }
}
