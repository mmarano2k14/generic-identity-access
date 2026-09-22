namespace IdentityAccess.Tests.Api
{
    /// <summary>Verifies HTTP correlation behavior.</summary>
    public sealed class CorrelationApiTests
    {
        /// <summary>Verifies that liveness responses include a non-empty server correlation id.</summary>
        [Fact]
        public async Task Responses_include_server_correlation_identifier()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(
                "/health/live",
                TestContext.Current.CancellationToken);

            Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
            Assert.False(string.IsNullOrWhiteSpace(Assert.Single(values)));
        }
    }
}
