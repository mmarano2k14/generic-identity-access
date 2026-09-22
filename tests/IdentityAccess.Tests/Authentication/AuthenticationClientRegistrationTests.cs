using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authentication
{

    public sealed class AuthenticationClientRegistrationTests
    {
        [Fact]
        public void Redirect_matching_is_exact()
        {
            var client = Client("https://example.test/auth/callback");
            Assert.True(client.AllowsRedirectUri("https://example.test/auth/callback"));
            Assert.False(client.AllowsRedirectUri("https://example.test/auth/callback/"));
            Assert.False(client.AllowsRedirectUri("https://EXAMPLE.test/auth/callback"));
            Assert.False(client.AllowsRedirectUri("https://example.test/auth/callback?next=/admin"));
        }

        [Theory]
        [InlineData("https://*.example.test/callback")]
        [InlineData("https://user@example.test/callback")]
        [InlineData("https://example.test/callback#fragment")]
        [InlineData("http://example.test/callback")]
        public void Unsafe_redirect_registrations_are_rejected(string uri)
        {
            Assert.Throws<ArgumentException>(() => Client(uri));
        }

        [Fact]
        public void Http_loopback_redirect_is_allowed_for_local_development()
        {
            var client = Client("http://127.0.0.1:3000/auth/callback");
            Assert.True(client.AllowsRedirectUri("http://127.0.0.1:3000/auth/callback"));
        }

        [Fact]
        public void Oidc_client_requires_the_openid_scope()
        {
            Assert.Throws<ArgumentException>(() => new AuthenticationClientRegistration(
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                ["https://example.test/auth/callback"],
                oidcEnabled: true,
                allowedOidcScopes: []));

            var client = new AuthenticationClientRegistration(
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                ["https://example.test/auth/callback"],
                oidcEnabled: true,
                allowedOidcScopes: ["openid"]);

            Assert.True(client.OidcEnabled);
            Assert.True(client.AllowsOidcScope("openid"));
            Assert.False(client.AllowsOidcScope("profile"));
        }

        [Fact]
        public void Unsupported_oidc_scopes_are_rejected()
        {
            Assert.Throws<ArgumentException>(() => new AuthenticationClientRegistration(
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                ["https://example.test/auth/callback"],
                oidcEnabled: true,
                allowedOidcScopes: ["openid", "profile"]));
        }

        private static AuthenticationClientRegistration Client(string redirect) => new(
            "web-client",
            new ApplicationKey("app-a"),
            "app-a-primary",
            [redirect],
            ["https://example.test/"]);
    }
}
