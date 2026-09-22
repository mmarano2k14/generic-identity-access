using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql;

namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class PostgreSqlConnectionFactoryHardeningTests
    {
        [Fact]
        public async Task Invalid_connection_string_does_not_poison_destination_registration_or_disclose_secret()
        {
            const string marker = "DO_NOT_ECHO_SECRET_VALUE";
            var resolver = new CountingConnectionSecretResolver(
                $"Host=localhost;Password={marker};unsupported-key=value");

            await using var factory = new PostgreSqlConnectionFactory(
                resolver,
                new PostgreSqlStorageOptions());

            var route = new ResolvedDatabaseRoute(
                new DatabaseRouteRequest(
                    new ApplicationKey("app-a"),
                    Guid.Parse("41000000-0000-0000-0000-000000000001")),
                "identity-a",
                new ConnectionSecretReference("env:IDENTITY_ACCESS_TEST_DATABASE"),
                configurationRevision: 1,
                routeVersion: 1);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var error = await Assert.ThrowsAsync<PostgreSqlStorageException>(
                    () => factory.OpenAsync(route, CancellationToken.None).AsTask());

                Assert.Equal(
                    PostgreSqlStorageFailure.InvalidConnectionString,
                    error.Failure);
                Assert.DoesNotContain(marker, error.ToString(), StringComparison.Ordinal);
                Assert.Null(error.InnerException);
                Assert.Equal(0, factory.RegisteredDestinationCount);
            }

            Assert.Equal(2, resolver.ResolveCount);
        }
    }
}
