using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Protects local-session assurance invariants independently from persistence.</summary>
    public sealed class AuthenticationAssuranceTests
    {
        [Fact]
        public void Password_assurance_contains_only_pwd()
        {
            var verifiedAt = DateTimeOffset.UtcNow;
            var assurance = AuthenticationAssurance.Password(verifiedAt);

            Assert.Equal(AuthenticationAssuranceLevel.PasswordOnly, assurance.Level);
            Assert.Equal(new[] { AuthenticationMethodReferences.Password }, assurance.Methods);
            Assert.Equal(verifiedAt, assurance.VerifiedAt);
            Assert.Equal("urn:generic-identity-access:acr:password", assurance.Acr);
        }

        [Fact]
        public void Additional_factor_upgrades_to_mfa_and_preserves_method_provenance()
        {
            var passwordAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var factorAt = passwordAt.AddMinutes(2);
            var assurance = AuthenticationAssurance.Password(passwordAt)
                .WithFactor(AuthenticationMethodReferences.OneTimePassword, factorAt);

            Assert.Equal(AuthenticationAssuranceLevel.MultiFactor, assurance.Level);
            Assert.Contains(AuthenticationMethodReferences.Password, assurance.Methods);
            Assert.Contains(AuthenticationMethodReferences.MultiFactor, assurance.Methods);
            Assert.Contains(AuthenticationMethodReferences.OneTimePassword, assurance.Methods);
            Assert.Equal(factorAt, assurance.VerifiedAt);
            Assert.Equal("urn:generic-identity-access:acr:mfa", assurance.Acr);
        }

        [Fact]
        public void Multi_factor_assurance_requires_a_concrete_factor_method()
        {
            Assert.Throws<ArgumentException>(() =>
                new AuthenticationAssurance(
                    AuthenticationAssuranceLevel.MultiFactor,
                    [AuthenticationMethodReferences.Password, AuthenticationMethodReferences.MultiFactor],
                    DateTimeOffset.UtcNow));
        }

        [Fact]
        public void Freshness_is_bounded_by_the_configured_maximum_age()
        {
            var now = DateTimeOffset.UtcNow;
            var assurance = AuthenticationAssurance.Password(now.AddMinutes(-10))
                .WithFactor(AuthenticationMethodReferences.ProofOfPossession, now.AddMinutes(-6));

            Assert.False(assurance.IsRecentMultiFactor(now, TimeSpan.FromMinutes(5)));
            Assert.True(assurance.IsRecentMultiFactor(now, TimeSpan.FromMinutes(10)));
        }
    }
}
