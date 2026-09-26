using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;

namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class ManagedPolicyCatalogPersistenceContractTests
    {
        [Fact]
        public async Task Managed_policy_store_rejects_cross_scope_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var reference = new ManagedPolicyReference(scope, new ApplicationKey("admin-app"), Guid.NewGuid());
            var policy = new ManagedPolicy(reference, new ManagedPolicyKey("iam-read-only"), "IAM Read Only");
            var store = new PostgreSqlManagedPolicyStore(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CreateAsync(Route(Guid.NewGuid()), policy, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Managed_policy_version_store_rejects_cross_scope_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var application = new ApplicationKey("admin-app");
            var reference = new ManagedPolicyReference(scope, application, Guid.NewGuid());
            var version = new ManagedPolicyVersion(
                new ManagedPolicyVersionReference(reference, 1),
                new ApplicationSecurityModelReference(scope, application, 2));
            var store = new PostgreSqlManagedPolicyVersionStore(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CreateAsync(Route(Guid.NewGuid()), version, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Managed_policy_statement_store_rejects_cross_scope_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var application = new ApplicationKey("admin-app");
            var reference = new ManagedPolicyReference(scope, application, Guid.NewGuid());
            var versionReference = new ManagedPolicyVersionReference(reference, 1);
            var statement = new ManagedPolicyStatement(
                Guid.NewGuid(),
                versionReference,
                new ApplicationSecurityModelReference(scope, application, 2),
                new CapabilityPattern("identity-access", "user", "read"));
            var store = new PostgreSqlManagedPolicyStatementStore(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.AddAsync(Route(Guid.NewGuid()), statement, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Managed_policy_catalog_is_embedded_as_migration_0023_without_tenant_ownership()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0023_managed_policy_catalog.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            Assert.Contains("managed_policies", sql, StringComparison.Ordinal);
            Assert.Contains("managed_policy_versions", sql, StringComparison.Ordinal);
            Assert.Contains("managed_policy_statements", sql, StringComparison.Ordinal);
            Assert.Contains("default_version", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("tenant_id", sql, StringComparison.Ordinal);
        }


        [Fact]
        public async Task Managed_policy_publication_rejects_cross_scope_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var application = new ApplicationKey("admin-app");
            var reference = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(scope, application, Guid.NewGuid()),
                1);
            var store = new PostgreSqlManagedPolicyVersionStore(new MustNotOpenConnectionFactory());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.PublishAsync(Route(Guid.NewGuid()), reference, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Managed_policy_version_tracks_draft_and_published_state()
        {
            var scope = Guid.NewGuid();
            var application = new ApplicationKey("admin-app");
            var reference = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(scope, application, Guid.NewGuid()),
                2);
            var model = new ApplicationSecurityModelReference(scope, application, 4);
            var publishedAt = DateTimeOffset.UtcNow;

            var draft = new ManagedPolicyVersion(reference, model);
            var published = new ManagedPolicyVersion(reference, model, publishedAt);

            Assert.False(draft.IsPublished);
            Assert.Null(draft.PublishedAt);
            Assert.True(published.IsPublished);
            Assert.Equal(publishedAt, published.PublishedAt);
        }

        [Fact]
        public void Migration_0025_freezes_published_versions_and_requires_publication_for_binding()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resource = Assert.Single(assembly.GetManifestResourceNames(), name =>
                name.EndsWith("0025_managed_policy_publication.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            Assert.Contains("published_at", sql, StringComparison.Ordinal);
            Assert.Contains("guard_managed_policy_version_mutation", sql, StringComparison.Ordinal);
            Assert.Contains("guard_managed_policy_statement_mutation", sql, StringComparison.Ordinal);
            Assert.Contains("guard_managed_policy_default_version", sql, StringComparison.Ordinal);
            Assert.Contains("guard_managed_policy_binding_publication", sql, StringComparison.Ordinal);
            Assert.Contains("SET published_at = pv.created_at", sql, StringComparison.Ordinal);
            Assert.Contains("managed_group_policy_bindings", sql, StringComparison.Ordinal);
        }

        [Fact]
        public void Managed_policy_storage_contracts_require_explicit_cancellation_tokens()
        {
            var contracts = new[]
            {
                typeof(IManagedPolicyStore),
                typeof(IManagedPolicyVersionStore),
                typeof(IManagedPolicyStatementStore)
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

        private static ResolvedDatabaseRoute Route(Guid scope) => new(
            new DatabaseRouteRequest(new ApplicationKey("admin-app"), scope),
            "identity-default",
            new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
            1,
            1);
    }
}
