using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.ConfigurationRouting;

namespace IdentityAccess.Tests;

public sealed class AuthenticationDirectoryLocatorTests
{
    [Fact]
    public async Task Registered_context_locates_one_directory_without_an_email_or_tenant_scan()
    {
        IAuthenticationDirectoryLocator locator = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        var location = await locator.LocateAsync(new ApplicationKey("app-a"), "app-a-secondary", TestContext.Current.CancellationToken);
        Assert.Equal(RoutingFixture.ScopeB, location.Route.Request.IdentityScopeId);
        Assert.Equal("identity-b", location.Route.DestinationKey);
        Assert.Equal("app-a-secondary", location.AuthenticationContextKey);
    }

    [Theory]
    [InlineData("unregistered")]
    [InlineData("APP-A-PRIMARY")]
    [InlineData("unknown@example.test")]
    public async Task Unknown_context_never_falls_back_to_the_application_first_directory(string key)
    {
        var locator = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        var error = await Assert.ThrowsAsync<DatabaseRouteException>(async () =>
            await locator.LocateAsync(new ApplicationKey("app-a"), key, TestContext.Current.CancellationToken));
        Assert.Equal(DatabaseRouteFailure.AuthenticationContextNotFound, error.Code);
        Assert.DoesNotContain(key, error.Message);
    }

    [Fact]
    public async Task Context_registered_for_another_application_is_rejected()
    {
        var locator = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        var error = await Assert.ThrowsAsync<DatabaseRouteException>(async () =>
            await locator.LocateAsync(new ApplicationKey("app-b"), "app-a-primary", TestContext.Current.CancellationToken));
        Assert.Equal(DatabaseRouteFailure.ApplicationContextMismatch, error.Code);
    }

    [Theory]
    [InlineData("authenticationContexts", DatabaseRouteFailure.AuthenticationContextDisabled)]
    [InlineData("routes", DatabaseRouteFailure.RouteDisabled)]
    [InlineData("destinations", DatabaseRouteFailure.DestinationDisabled)]
    public async Task Bootstrap_respects_context_route_and_destination_disabling(string collection, DatabaseRouteFailure expected)
    {
        var locator = ConfigurationRoutingProvider.Parse(RoutingFixture.Change(d => d[collection]![0]!["state"] = "disabled"));
        var error = await Assert.ThrowsAsync<DatabaseRouteException>(async () =>
            await locator.LocateAsync(new ApplicationKey("app-a"), "app-a-primary", TestContext.Current.CancellationToken));
        Assert.Equal(expected, error.Code);
    }

    [Fact]
    public async Task Directory_location_is_cancellable()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var locator = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await locator.LocateAsync(new ApplicationKey("app-a"), "app-a-primary", cancelled.Token));
    }
}
