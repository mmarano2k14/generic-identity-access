using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlAssignedCapabilityReaderTests
    {
        [Fact]
        public async Task Reader_rejects_cross_scope_before_opening_connection()
        {
            var routeScope = Guid.NewGuid();
            var tenantScope = Guid.NewGuid();
            var reader = new PostgreSqlAssignedCapabilityReader(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ListAsync(
                Route(routeScope),
                new TenantReference(tenantScope, Guid.NewGuid()),
                new SubjectReference(tenantScope, Guid.NewGuid()),
                new ApplicationKey("app-a"),
                null,
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Assignment_projection_indexes_are_embedded_as_fourth_migration()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0004_assignment_projection_indexes.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            Assert.Contains("tenant_memberships", sql, StringComparison.Ordinal);
            Assert.Contains("group_policy_bindings", sql, StringComparison.Ordinal);
            Assert.Contains("policy_statements", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("ALLOW", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static ResolvedDatabaseRoute Route(Guid scope) => new(
            new DatabaseRouteRequest(new ApplicationKey("app-a"), scope),
            "identity-default",
            new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
            1,
            1);


    }
}
