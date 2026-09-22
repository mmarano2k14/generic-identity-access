using System.Text.Json;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{

    public sealed class RoutingContractTests
    {
        [Fact]
        public void Route_request_requires_explicit_application_and_identity_scope()
        {
            Assert.Throws<ArgumentNullException>(() => new DatabaseRouteRequest(null!, RoutingFixture.ScopeA));
            Assert.Throws<ArgumentException>(() => new DatabaseRouteRequest(new ApplicationKey("app-a"), Guid.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DatabaseRouteRequest(new ApplicationKey("app-a"), RoutingFixture.ScopeA, (IdentityDataSet)99));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Resolved_routes_require_positive_configuration_and_route_versions(long version)
        {
            var reference = new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_A");
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ResolvedDatabaseRoute(RoutingFixture.Request(), "identity-a", reference, version, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ResolvedDatabaseRoute(RoutingFixture.Request(), "identity-a", reference, 1, version));
        }

        [Fact]
        public void Secret_reference_and_route_do_not_print_or_serialize_secret_locator()
        {
            const string value = "env:IDENTITY_ACCESS_POSTGRES_A";
            var reference = new ConnectionSecretReference(value);
            var route = new ResolvedDatabaseRoute(RoutingFixture.Request(), "identity-a", reference, 7, 3);
            Assert.Equal(value, reference.Value);
            Assert.DoesNotContain(value, reference.ToString());
            Assert.DoesNotContain(value, route.ToString());
            Assert.DoesNotContain(value, JsonSerializer.Serialize(reference));
            Assert.DoesNotContain(value, JsonSerializer.Serialize(route));
            Assert.Equal("postgresql", route.Provider);
            Assert.Equal("active", route.AdministrativeState);
        }

        [Theory]
        [InlineData("")]
        [InlineData("env:")]
        [InlineData(":password")]
        [InlineData("ENV:KEY")]
        [InlineData("env:secret with spaces")]
        [InlineData("Host=localhost;Password=secret")]
        [InlineData("env:KEY?credential=secret")]
        public void Secret_reference_rejects_connection_strings_and_malformed_locators(string value) =>
            Assert.Throws<ArgumentException>(() => new ConnectionSecretReference(value));

        [Fact]
        public void Routing_contracts_are_not_part_of_the_public_profile_contract_assembly()
        {
            var types = typeof(IdentityAccess.Contracts.LivenessResponse).Assembly.GetExportedTypes();
            Assert.DoesNotContain(types, type => type.Namespace?.Contains("Routing", StringComparison.Ordinal) == true);
        }
    }
}
