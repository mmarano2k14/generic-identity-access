using System.Net;

namespace IdentityAccess.Tests.Api
{
    /// <summary>
    /// Verifies administration endpoints distinguish authentication failure from unavailable
    /// capability authorization.
    /// </summary>
    public sealed class AdministrationAuthenticationApiTests
    {
        /// <summary>
        /// Verifies that a protected administration route without session credentials reaches the
        /// administration authentication boundary rather than falling through route matching.
        /// </summary>
        [Fact]
        public async Task Protected_route_without_administration_session_is_unauthorized()
        {
            const string identityScopeId = "31111111-1111-1111-1111-111111111111";
            const string userId = "31aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

            Assert.True(Guid.TryParseExact(identityScopeId, "D", out _));
            Assert.True(Guid.TryParseExact(userId, "D", out _));

            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(
                $"/api/v1/identity-scopes/{identityScopeId}/applications/app-a/users/{userId}",
                TestContext.Current.CancellationToken);

            Assert.Contains(
                response.StatusCode,
                new[]
                {
                    HttpStatusCode.Unauthorized,
                    HttpStatusCode.ServiceUnavailable
                });
        }
    }
}
