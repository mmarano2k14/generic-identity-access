using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.ConfigurationRouting;

namespace IdentityAccess.Tests;

public sealed class DatabaseRouteResolverTests
{
    [Fact]
    public async Task One_application_uses_multiple_destinations_without_one_database_per_application()
    {
        IDatabaseRouteResolver resolver = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        var a = await resolver.ResolveAsync(RoutingFixture.Request(), TestContext.Current.CancellationToken);
        var b = await resolver.ResolveAsync(RoutingFixture.Request(RoutingFixture.ScopeB), TestContext.Current.CancellationToken);
        Assert.Equal("identity-a", a.DestinationKey);
        Assert.Equal("identity-b", b.DestinationKey);
        Assert.Equal(7L, a.ConfigurationRevision);
        Assert.Equal(3L, a.RouteVersion);
        Assert.Equal("env:IDENTITY_ACCESS_POSTGRES_A", a.ConnectionSecretReference.Value);
    }

    [Fact]
    public async Task One_destination_can_host_distinct_scopes_without_merging_their_identities()
    {
        var resolver = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        var a = await resolver.ResolveAsync(RoutingFixture.Request(), TestContext.Current.CancellationToken);
        var c = await resolver.ResolveAsync(RoutingFixture.Request(RoutingFixture.ScopeC, "app-b"), TestContext.Current.CancellationToken);
        Assert.Equal(a.DestinationKey, c.DestinationKey);
        var localId = Guid.NewGuid();
        Assert.NotEqual(new SubjectReference(a.Request.IdentityScopeId, localId), new SubjectReference(c.Request.IdentityScopeId, localId));
    }

    [Fact]
    public async Task Missing_route_never_uses_first_default_or_other_application_destination()
    {
        var resolver = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        foreach (var request in new[]
        {
            RoutingFixture.Request(Guid.NewGuid()),
            RoutingFixture.Request(RoutingFixture.ScopeA, "app-b"),
            RoutingFixture.Request(RoutingFixture.ScopeA, "unknown")
        })
        {
            var error = await Assert.ThrowsAsync<DatabaseRouteException>(async () =>
                await resolver.ResolveAsync(request, TestContext.Current.CancellationToken));
            Assert.Equal(DatabaseRouteFailure.RouteNotFound, error.Code);
        }
    }

    [Theory]
    [InlineData("routes", DatabaseRouteFailure.RouteDisabled)]
    [InlineData("destinations", DatabaseRouteFailure.DestinationDisabled)]
    public async Task Administrative_disabling_blocks_resolution_without_fallback(string collection, DatabaseRouteFailure expected)
    {
        var resolver = ConfigurationRoutingProvider.Parse(RoutingFixture.Change(d => d[collection]![0]!["state"] = "disabled"));
        var error = await Assert.ThrowsAsync<DatabaseRouteException>(async () =>
            await resolver.ResolveAsync(RoutingFixture.Request(), TestContext.Current.CancellationToken));
        Assert.Equal(expected, error.Code);
        var unrelated = await resolver.ResolveAsync(RoutingFixture.Request(RoutingFixture.ScopeB), TestContext.Current.CancellationToken);
        Assert.Equal("identity-b", unrelated.DestinationKey);
    }

    [Fact]
    public async Task Sharing_a_scope_requires_an_explicit_registration_for_the_second_application()
    {
        var json = RoutingFixture.Change(d =>
        {
            var shared = d["routes"]![0]!.DeepClone();
            shared["applicationKey"] = "app-b";
            d["routes"]!.AsArray().Add(shared);
        });
        var resolver = ConfigurationRoutingProvider.Parse(json);
        var result = await resolver.ResolveAsync(RoutingFixture.Request(RoutingFixture.ScopeA, "app-b"), TestContext.Current.CancellationToken);
        Assert.Equal("identity-a", result.DestinationKey);
        Assert.Equal("app-b", result.Request.Application.Value);
    }

    [Fact]
    public async Task Concurrent_resolutions_never_share_a_mutable_current_destination()
    {
        var resolver = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        var tasks = Enumerable.Range(0, 128).Select(i => Task.Run(async () =>
        {
            var useA = i % 2 == 0;
            var result = await resolver.ResolveAsync(RoutingFixture.Request(useA ? RoutingFixture.ScopeA : RoutingFixture.ScopeB),
                TestContext.Current.CancellationToken);
            Assert.Equal(useA ? "identity-a" : "identity-b", result.DestinationKey);
            Assert.Equal(useA ? RoutingFixture.ScopeA : RoutingFixture.ScopeB, result.Request.IdentityScopeId);
            return result;
        }, TestContext.Current.CancellationToken));
        var results = await Task.WhenAll(tasks);
        Assert.Equal(128, results.Length);
    }

    [Fact]
    public async Task An_existing_operation_keeps_its_route_when_a_new_revision_is_built()
    {
        var oldProvider = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        var oldRoute = await oldProvider.ResolveAsync(RoutingFixture.Request(), TestContext.Current.CancellationToken);
        var newProvider = ConfigurationRoutingProvider.Parse(RoutingFixture.Change(d =>
        {
            d["revision"] = 8;
            d["routes"]![0]!["destinationKey"] = "identity-b";
            d["routes"]![0]!["version"] = 4;
        }));
        var newRoute = await newProvider.ResolveAsync(RoutingFixture.Request(), TestContext.Current.CancellationToken);
        Assert.Equal("identity-a", oldRoute.DestinationKey);
        Assert.Equal(7L, oldRoute.ConfigurationRevision);
        Assert.Equal("identity-b", newRoute.DestinationKey);
        Assert.Equal(8L, newRoute.ConfigurationRevision);
        Assert.Equal(oldRoute.Request, newRoute.Request);
    }

    [Fact]
    public async Task Cancellation_is_not_translated_to_a_missing_route()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var resolver = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await resolver.ResolveAsync(RoutingFixture.Request(), cancelled.Token));
    }
}
