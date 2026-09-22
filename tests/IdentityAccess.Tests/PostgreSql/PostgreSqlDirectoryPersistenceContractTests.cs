using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using IdentityAccess.Infrastructure.PostgreSql.Directory;

namespace IdentityAccess.Tests.PostgreSql
{

    public sealed class PostgreSqlDirectoryPersistenceContractTests
    {
        [Fact]
        public async Task Tenant_store_rejects_cross_scope_route_before_opening_connection()
        {
            var store = new PostgreSqlTenantDirectoryStore(new MustNotOpenConnectionFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.GetAsync(Route(Guid.NewGuid()), new TenantReference(Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Membership_store_rejects_cross_scope_route_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var membership = new TenantMembership(Guid.NewGuid(), new TenantReference(scope, Guid.NewGuid()),
                new SubjectReference(scope, Guid.NewGuid()));
            var store = new PostgreSqlTenantMembershipStore(new MustNotOpenConnectionFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CreateAsync(Route(Guid.NewGuid()), membership, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Group_store_rejects_cross_scope_route_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var group = new UserGroup(new GroupReference(new TenantReference(scope, Guid.NewGuid()),
                new ApplicationKey("app-a"), Guid.NewGuid()), "Operators");
            var store = new PostgreSqlUserGroupStore(new MustNotOpenConnectionFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.CreateAsync(Route(Guid.NewGuid()), group, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Group_membership_store_rejects_cross_scope_route_before_opening_connection()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var membership = new TenantMembership(Guid.NewGuid(), tenant, new SubjectReference(scope, Guid.NewGuid()));
            var group = new UserGroup(new GroupReference(tenant, new ApplicationKey("app-a"), Guid.NewGuid()), "Operators");
            var edge = GroupMembership.Create(group, membership);
            var store = new PostgreSqlGroupMembershipStore(new MustNotOpenConnectionFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.AddAsync(Route(Guid.NewGuid()), edge, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Persisted_group_edge_can_be_restored_without_claiming_current_authorization()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var group = new GroupReference(tenant, new ApplicationKey("app-a"), Guid.NewGuid());
            var subject = new SubjectReference(scope, Guid.NewGuid());
            var membershipId = Guid.NewGuid();

            var edge = GroupMembership.Restore(group, membershipId, subject);

            Assert.Equal(group, edge.Group);
            Assert.Equal(membershipId, edge.TenantMembershipId);
            Assert.Equal(subject, edge.Subject);
        }

        [Fact]
        public void Persisted_group_edge_restore_rejects_cross_scope_subject()
        {
            var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
            var group = new GroupReference(tenant, new ApplicationKey("app-a"), Guid.NewGuid());
            Assert.Throws<ArgumentException>(() => GroupMembership.Restore(group, Guid.NewGuid(),
                new SubjectReference(Guid.NewGuid(), Guid.NewGuid())));
        }

        [Fact]
        public void Directory_query_indexes_are_embedded_as_second_migration()
        {
            var assembly = typeof(IdentityAccess.Infrastructure.PostgreSql.PostgreSqlSchemaMigrator).Assembly;
            var resources = assembly.GetManifestResourceNames();
            var resource = Assert.Single(resources, name =>
                name.EndsWith("0002_directory_query_indexes.sql", StringComparison.Ordinal));
            using var stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resource));
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            Assert.Contains("ix_group_memberships_group", sql, StringComparison.Ordinal);
            Assert.Contains("identity_scope_id", sql, StringComparison.Ordinal);
        }

        private static ResolvedDatabaseRoute Route(Guid scope) => new(
            new DatabaseRouteRequest(new ApplicationKey("app-a"), scope),
            "identity-default",
            new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
            1,
            1);


    }
}
