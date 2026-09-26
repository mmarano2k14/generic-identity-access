using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;

namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class ManagedPolicyBindingPersistenceContractTests
    {
        [Fact]
        public async Task Binding_store_rejects_cross_scope_before_opening_connection()
        {
            var binding = Binding(Guid.NewGuid());
            var store = new PostgreSqlManagedGroupPolicyBindingStore(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.ListAsync(
                    Route(Guid.NewGuid()),
                    binding.Group,
                    TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Binding_mutation_store_rejects_cross_scope_before_opening_connection()
        {
            var binding = Binding(Guid.NewGuid());
            var store = new PostgreSqlManagedGroupPolicyBindingMutationStore(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.AddIfActiveAsync(
                    Route(Guid.NewGuid()),
                    binding,
                    TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Migration_0024_keeps_tenant_on_binding_but_not_on_policy_foreign_key()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0024_managed_policy_bindings.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            Assert.Contains("managed_group_policy_bindings", sql, StringComparison.Ordinal);
            Assert.Contains("tenant_id uuid NOT NULL", sql, StringComparison.Ordinal);
            Assert.Contains(
                "(identity_scope_id, application_key, policy_id, policy_version)",
                sql,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "(identity_scope_id, tenant_id, application_key, policy_id, policy_version)\n        REFERENCES identity_access.managed_policy_versions",
                sql,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Managed_binding_storage_contracts_require_explicit_cancellation_tokens()
        {
            var contracts = new[]
            {
                typeof(IManagedGroupPolicyBindingStore),
                typeof(IManagedGroupPolicyBindingMutationStore)
            };

            foreach (var contract in contracts)
            {
                foreach (var method in contract.GetMethods())
                {
                    var last = Assert.Single(method.GetParameters().TakeLast(1));
                    Assert.Equal(typeof(CancellationToken), last.ParameterType);
                    Assert.False(last.IsOptional);
                }
            }
        }

        private static ManagedGroupPolicyBinding Binding(Guid scope)
        {
            var application = new ApplicationKey("admin-app");
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var group = new GroupReference(tenant, application, Guid.NewGuid());
            var policy = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(scope, application, Guid.NewGuid()),
                1);
            return ManagedGroupPolicyBinding.Restore(group, policy, null, false);
        }

        private static ResolvedDatabaseRoute Route(Guid scope) => new(
            new DatabaseRouteRequest(new ApplicationKey("admin-app"), scope),
            "identity-default",
            new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
            1,
            1);
    }
}
