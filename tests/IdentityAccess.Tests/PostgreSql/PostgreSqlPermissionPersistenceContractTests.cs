using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlPermissionPersistenceContractTests
    {
        [Fact]
        public async Task Security_model_store_rejects_cross_scope_before_opening_connection()
        {
            var store = new PostgreSqlApplicationSecurityModelStore(new MustNotOpenConnectionFactory());
            var model = new ApplicationSecurityModelReference(Guid.NewGuid(), new ApplicationKey("app-a"), 1);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CreateModelAsync(Route(Guid.NewGuid()), model, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Policy_store_rejects_cross_scope_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var policy = new PermissionPolicy(new PermissionPolicyReference(
                new TenantReference(scope, Guid.NewGuid()), new ApplicationKey("app-a"), Guid.NewGuid()), "Operators");
            var store = new PostgreSqlPermissionPolicyStore(new MustNotOpenConnectionFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CreateAsync(Route(Guid.NewGuid()), policy, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Statement_store_rejects_cross_scope_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var app = new ApplicationKey("app-a");
            var policy = new PermissionPolicyReference(new TenantReference(scope, Guid.NewGuid()), app, Guid.NewGuid());
            var model = new ApplicationSecurityModelReference(scope, app, 1);
            var statement = new PolicyStatement(Guid.NewGuid(), policy, model,
                new CapabilityKey("billing", "invoice", "read"));
            var store = new PostgreSqlPolicyStatementStore(new MustNotOpenConnectionFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.AddAsync(Route(Guid.NewGuid()), statement, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Binding_store_rejects_cross_scope_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var app = new ApplicationKey("app-a");
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var binding = GroupPolicyBinding.Restore(
                new GroupReference(tenant, app, Guid.NewGuid()),
                new PermissionPolicyReference(tenant, app, Guid.NewGuid()));
            var store = new PostgreSqlGroupPolicyBindingStore(new MustNotOpenConnectionFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.AddAsync(Route(Guid.NewGuid()), binding, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Permission_foundation_is_embedded_as_third_migration()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0003_permission_policy_foundation.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            Assert.Contains("application_security_models", sql, StringComparison.Ordinal);
            Assert.Contains("permission_policies", sql, StringComparison.Ordinal);
            Assert.Contains("policy_statements", sql, StringComparison.Ordinal);
            Assert.Contains("group_policy_bindings", sql, StringComparison.Ordinal);
            Assert.Contains("identity_scope_id", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("trn", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static ResolvedDatabaseRoute Route(Guid scope) => new(
            new DatabaseRouteRequest(new ApplicationKey("app-a"), scope),
            "identity-default",
            new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
            1,
            1);


    }
}
