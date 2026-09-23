using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    public sealed class WebAuthnProviderOptionsTests
    {
        [Fact]
        public void Options_require_origins_within_relying_party_boundary()
        {
            Assert.Throws<ArgumentException>(() =>
                new WebAuthnProviderOptions(
                    "example.test",
                    "Example",
                    ["https://attacker.test"],
                    WebAuthnProviderOptions.DefaultChallengeLifetimeSeconds));
        }

        [Fact]
        public void Localhost_http_is_allowed_for_development()
        {
            var options = new WebAuthnProviderOptions(
                "localhost",
                "Local Identity Access",
                ["http://localhost:3000"],
                WebAuthnProviderOptions.DefaultChallengeLifetimeSeconds);

            Assert.Contains("http://localhost:3000", options.AllowedOrigins);
        }
    }
}
