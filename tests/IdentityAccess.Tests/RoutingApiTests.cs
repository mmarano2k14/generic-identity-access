using System.Net;
using System.Net.Http.Json;
using IdentityAccess.Application.Routing;
using IdentityAccess.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Tests;

public sealed class RoutingApiTests
{
    [Fact]
    public async Task Valid_file_registers_server_resolver_but_never_claims_storage_or_security_ready()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, RoutingFixture.Json);
            using var host = CreateHost("configuration", path);
            using var client = host.CreateClient();
            var resolver = host.Services.GetRequiredService<IDatabaseRouteResolver>();
            var route = await resolver.ResolveAsync(RoutingFixture.Request(), TestContext.Current.CancellationToken);
            Assert.Equal("identity-a", route.DestinationKey);
            Assert.Same(resolver, host.Services.GetRequiredService<IAuthenticationDirectoryLocator>());
            using var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var ready = await response.Content.ReadFromJsonAsync<ReadinessResponse>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(ready);
            Assert.False(ready.Ready);
            Assert.DoesNotContain("database-routing", ready.BlockingCapabilities);
            Assert.Contains("postgresql-persistence", ready.BlockingCapabilities);
            Assert.Contains("authentication", ready.BlockingCapabilities);
            var info = await client.GetFromJsonAsync<ServiceInfoResponse>("/api/v1/system/info", TestContext.Current.CancellationToken);
            Assert.NotNull(info);
            Assert.False(info.StorageConfigured);
            Assert.False(info.AuthenticationConfigured);
            Assert.False(info.AuthorizationConfigured);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Unconfigured_service_has_no_resolver_and_remains_explicitly_blocked()
    {
        using var host = CreateHost("none", null);
        using var client = host.CreateClient();
        Assert.Null(host.Services.GetService<IDatabaseRouteResolver>());
        // Readiness intentionally uses a non-success HTTP status; read it without EnsureSuccessStatusCode.
        using var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ReadinessResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Contains("database-routing", body.BlockingCapabilities);
    }

    [Theory]
    [InlineData("none", "unused.json", "ConflictingProviderSelection")]
    [InlineData("catalogue", null, "UnsupportedProvider")]
    [InlineData("configuration", null, "InvalidValue")]
    public void Invalid_provider_selection_prevents_host_startup(string provider, string? filePath, string expected)
    {
        using var host = CreateHost(provider, filePath);
        var error = Assert.ThrowsAny<Exception>(() => host.CreateClient());
        Assert.Contains(expected, error.ToString());
    }

    [Fact]
    public void Invalid_file_prevents_startup_instead_of_ignoring_the_configured_route_source()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{"DO_NOT_ECHO":invalid}""");
            using var host = CreateHost("configuration", path);
            var error = Assert.ThrowsAny<Exception>(() => host.CreateClient());
            Assert.Contains("InvalidJson", error.ToString());
            Assert.DoesNotContain("DO_NOT_ECHO", error.ToString());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Competing_source_configuration_is_rejected()
    {
        using var host = CreateHost("none", null, "catalogue-source");
        var error = Assert.ThrowsAny<Exception>(() => host.CreateClient());
        Assert.Contains("ConflictingProviderSelection", error.ToString());
    }

    [Fact]
    public async Task Diagnostics_do_not_expose_routes_secrets_or_bootstrap_registry()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, RoutingFixture.Json);
            using var host = CreateHost("configuration", path);
            using var client = host.CreateClient();
            foreach (var endpoint in new[] { "/health/live", "/health/ready", "/api/v1/system/info" })
            {
                using var response = await client.GetAsync(endpoint, TestContext.Current.CancellationToken);
                var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                Assert.DoesNotContain("IDENTITY_ACCESS_POSTGRES", text);
                Assert.DoesNotContain("identity-a", text.Replace("identity-access", "service", StringComparison.Ordinal));
                Assert.DoesNotContain("app-a-primary", text);
                Assert.DoesNotContain(RoutingFixture.ScopeA.ToString("D"), text);
            }
            foreach (var endpoint in new[] { "/api/v1/routing", "/api/v1/database-routes", "/api/v1/authentication-contexts" })
            {
                using var response = await client.GetAsync(endpoint, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        finally { File.Delete(path); }
    }

    private static WebApplicationFactory<Program> CreateHost(string provider, string? filePath, string? competingSource = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["IdentityAccess:Routing:Provider"] = provider,
                    ["IdentityAccess:Routing:FilePath"] = filePath
                };
                if (competingSource is not null)
                    values["IdentityAccess:Routing:Catalogue"] = competingSource;
                configuration.AddInMemoryCollection(values);
            });
        });
}
