using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using IdentityAccess.Application.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Tests.Authentication
{

    public sealed class AuthenticationApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Password_login_controller_exists_but_fails_closed_when_authentication_is_disabled()
        {
            using var scope = factory.Services.CreateScope();
            Assert.Null(scope.ServiceProvider.GetService<ILocalAuthenticationService>());

            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                "/api/v1/authentication/clients/web-client/password-login",
                new { loginIdentifier = "user@example.test", password = "not-a-real-password", redirectUri = "https://example.test/callback" },
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task Disabled_authentication_fails_closed_before_body_validation()
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                "/api/v1/authentication/clients/web-client/password-login",
                new { },
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task Swagger_documents_authentication_and_credential_controllers()
        {
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("/api/v1/authentication/clients/{clientId}/password-login", json, StringComparison.Ordinal);
            Assert.Contains("password-credential", json, StringComparison.Ordinal);
            Assert.Contains("/sessions/users/{userId}", json, StringComparison.Ordinal);
            Assert.Contains("/sessions/clients/{clientId}", json, StringComparison.Ordinal);
            Assert.Contains("/connect/authorize", json, StringComparison.Ordinal);
            Assert.Contains("/connect/token", json, StringComparison.Ordinal);
            Assert.Contains("/.well-known/openid-configuration", json, StringComparison.Ordinal);
            Assert.Contains("/.well-known/jwks.json", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Disabled_oidc_discovery_is_not_advertised()
        {
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(
                "/.well-known/openid-configuration",
                TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
    }
}
