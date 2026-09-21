using System.Net;
using System.Net.Http.Json;
using IdentityAccess.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityAccess.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}

public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Liveness_returns_200_without_claiming_production_readiness()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LivenessResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("alive", body.Status);
    }

    [Fact]
    public async Task Readiness_returns_503_with_explicit_missing_capabilities()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReadinessResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.False(body.Ready);
        Assert.Contains("postgresql-persistence", body.BlockingCapabilities);
        Assert.Contains("authentication", body.BlockingCapabilities);
    }

    [Fact]
    public async Task Service_descriptor_exposes_only_foundation_metadata()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system/info", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ServiceInfoResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("v1", body.ApiVersion);
        Assert.Equal("postgresql", body.StorageProvider);
        Assert.False(body.StorageConfigured);
        Assert.False(body.AuthenticationConfigured);
        Assert.False(body.AuthorizationConfigured);
    }

    [Fact]
    public async Task Diagnostic_responses_disable_storage_in_http_caches()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system/info", TestContext.Current.CancellationToken);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl.NoStore);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/tenants")]
    [InlineData("/api/v1/groups")]
    [InlineData("/connect/authorize")]
    [InlineData("/connect/token")]
    public async Task Unimplemented_identity_endpoints_are_not_exposed(string path)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Account_mutations_are_not_exposed_without_real_security()
    {
        using var client = factory.CreateClient();
        using var content = JsonContent.Create(new { displayName = "Not persisted" });
        using var response = await client.PostAsync("/api/v1/users", content, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Foundation_refuses_production_startup()
    {
        using var production = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var error = Assert.ThrowsAny<Exception>(() => production.CreateClient());
        Assert.Contains("Production security is not implemented", error.ToString());
    }
}
