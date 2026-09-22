using IdentityAccess.Application.Routing;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects authorization test fixtures from drifting away from the current immutable route
    /// contract.
    /// </summary>
    public sealed class AuthorizationTestFixtureContractTests
    {
        /// <summary>
        /// Verifies the route contract retains the complete placement snapshot constructor used by
        /// authorization tests.
        /// </summary>
        [Fact]
        public void Resolved_database_route_requires_complete_placement_snapshot()
        {
            var constructor = typeof(ResolvedDatabaseRoute)
                .GetConstructors()
                .Single();

            var parameterTypes = constructor
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    typeof(DatabaseRouteRequest),
                    typeof(string),
                    typeof(ConnectionSecretReference),
                    typeof(long),
                    typeof(long)
                },
                parameterTypes);
        }
    }
}
